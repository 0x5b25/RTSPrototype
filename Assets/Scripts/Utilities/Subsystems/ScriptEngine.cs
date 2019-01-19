using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using System.Dynamic;


namespace RTS.Subsys
{
    class ScriptEngine:RTS.Util.Singleton<ScriptEngine>, IDisposable
    {
        
        internal V8ScriptEngine runtime;

        internal Dictionary<string, ScriptModule> modules = new Dictionary<string, ScriptModule>();

        internal dynamic core;

        
        public ScriptEngine(bool pauseOnLoad)
        {
            V8ScriptEngineFlags flags = V8ScriptEngineFlags.None;
            flags = flags|V8ScriptEngineFlags.EnableDebugging;
            flags = pauseOnLoad ? flags | V8ScriptEngineFlags.AwaitDebuggerAndPauseOnStart : flags;

            //Load mamager scripts
            runtime = new V8ScriptEngine("runtime",flags,9000)
            {
                AllowReflection = true,           
            };
            runtime.Execute("core",ScriptEngineCore.core);
            core = runtime.Script.core;
            runtime.Script.core = null;
        }
        public ScriptEngine():this(false){ }

        public ScriptInstance InstantiateModule(string moduleName)
        {
            if (!modules.ContainsKey(moduleName))
                return null;
            ScriptModule mod = modules[moduleName];
            if (mod.instantiate == null)
                return null;

            dynamic inst = mod.instantiate();
            if (core.CheckIsNull(inst))
                return null;

            return new ScriptInstance(this,core.AddObjInstance(inst));
        }

        public bool LoadScriptAsModule(string path, string fileName)
        {
            //Check loaded module cache
            if (modules.ContainsKey(fileName))
                return true;
            //Parse and load module
            string content = Util.ReadFile(path, fileName);
            if(content != null && content != "")
            {
                ScriptModule mod = new ScriptModule(this, path, fileName);
                modules.Add(fileName, mod);
                mod.LoadScript(content);
                return true;
            }
            return false;
        }

        public DynamicObject ImportModule(string currentPath,string moduleName)
        {
            if (!modules.ContainsKey(moduleName))
            {
                if(!LoadScriptAsModule(currentPath,moduleName))
                    return null;
            }
            if (!modules[moduleName].isLoaded)
                throw new Exception("Module import loop while loading module:" + moduleName);
            return modules[moduleName].exported;
        }

        public dynamic ParseJSON(string content)
        {
            return runtime.Script.JSON.parse(content);
        }

        public string GenerateJSON(object obj)
        {
            return runtime.Script.JSON.stringify(obj);
        }

        public void Dispose()
        {
            runtime?.Dispose();
        }

        internal void InjectFuncs(V8ScriptEngine context)
        {
            //context.AddHostObject("module.import", new Func<string,DynamicObject>(ImportModule));
        }

    }

    class ScriptModule
    {
        internal DynamicObject exported;
        internal dynamic instantiate;

        internal string path;
        internal string name;

        internal bool isLoaded = false;

        ScriptEngine engine;

        internal ScriptModule(ScriptEngine engine, string path, string name)
        {
            this.path = path;
            this.name = name;
            this.engine = engine;
        }

        internal void LoadScript(string scriptContent)
        {
            //Prepare env
            exported = engine.core.CreateEmpty();
            Func<string, DynamicObject> importFunc = (string moduleName) => { return engine.ImportModule(path, moduleName); };
            engine.runtime.Script.module = new { import = importFunc, exports = exported, filename = name, filepath = path };

            //Load content
            string p = Path.Combine(path, name);
            p = Path.GetFullPath(p);
            DocumentInfo info = new DocumentInfo(new Uri(p));
            engine.runtime.Execute(info,WrapMod(scriptContent));
            instantiate = engine.core.GetProperty(exported, "instantiate");
            isLoaded = true;
        }

        string WrapMod(string content)
        {
            return "(function(module){"+content+"})(module);";
        }
    }

    class ScriptInstance : IDisposable
    {
        public dynamic Script { get { return obj.val; } }

        internal dynamic obj;//object instance holder

        ScriptEngine engine;

        internal ScriptInstance(ScriptEngine engine, DynamicObject objInstance)
        {
            this.engine = engine;
            this.obj = objInstance;
        }

        public void Dispose()
        {
            engine.runtime.Script.ReleaseObjInstance(obj);
            obj.val.Dispose();
            obj.Dispose();
        }
    }

    class ScriptEngineCore
    {
        internal static readonly string core = @"
var core = {
    CreateEmpty:function (){
        let temp = {};
        return temp;
    },

    CheckProperty:function(obj,name){
        if(obj == null) return false;
        return obj[name] != null;
    },

    GetProperty:function(obj, name){
        if(obj == null || obj[name] == undefined) return null;
        return obj[name];
    },

    AddObjInstance:function(src){
        //let cpy = Object.assign({},src);
        return this.objs.Add(src);
    },

    ReleaseObjInstance:function(obj){
        return this.objs.Remove(obj);
    },

    CheckIsNull:function(obj){return obj == null || obj == undefined;},

    objs:{
        count:0,
        front:null,
        back:null,

        Add:function(obj){
            let holder = this.CreateHolder(obj);
            holder.prev = this.back;
            if(this.back != null){
                this.back.next = holder;
            }else this.front = holder;
            this.back = holder;
            this.count++;
            return holder;
        },

        Remove:function(obj){
            if(this.count <= 0) return false;

            if(obj.prev != null){
                obj.prev.next = obj.next;
            }else{
                this.front = obj.next;
            }

            if(obj.next != null){
                obj.next.prev = obj.prev;
            }else{
                obj.back = obj.prev;
            }

            this.count--;
            return true;
        },

        CreateHolder:function(obj){
            return{
                prev:null,
                next:null,
                val:obj,
            }
        }
    }
}

var module = null;";
    }
}
