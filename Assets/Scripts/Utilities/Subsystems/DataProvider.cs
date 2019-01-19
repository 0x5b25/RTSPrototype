using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using RTS.AssetTypes;

namespace RTS.Subsys
{
    public class DataProvider:Util.Singleton<DataProvider>
    {
        public RTSConfig config { get
            {
                if (_config == null) _config = GetAsset<RTSConfig>("Config", "Config");
                return _config;
            }
        }
        RTSConfig _config = null;

        public ComponentDB componentDatabase { get
            {
                if (_cpntdb == null) _cpntdb = GetAsset<ComponentDB>("Database", "ComponentDatabase");
                return _cpntdb;
            }
        }
        ComponentDB _cpntdb;

        public GameObject GetPrefab(string name)
        {
#if UNITY_EDITOR
            CreateResourceFolder("Prefab");
#endif
            return Resources.Load<GameObject>("Prefab/" + name);
        }

        T GetAsset<T>(string path, string name)where T:ScriptableObject
        {
            string tempP = path.Trim('/');
            string pathname = tempP == "" ? name : tempP + '/' + name;
#if UNITY_EDITOR
            T asset = Resources.Load<T>(pathname);
            if (asset == null) asset = CreateAsset<T>(path, name);
            return asset;
#else
            return Resources.Load<T>(pathname);
#endif

        }

#if UNITY_EDITOR
        T CreateAsset<T>(string assetPath, string name) where T:ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            assetPath = assetPath.Trim('/');

            CreateResourceFolder(assetPath);
            AssetDatabase.CreateAsset(asset, "Assets/Resources/" + assetPath + "/" + name + ".asset");
            AssetDatabase.SaveAssets();

            return asset;
        }

        void CreateResourceFolder(string relativePath)
        {
            relativePath = relativePath.Replace("\\", "/");
            relativePath = relativePath.Trim('/');
            string[] folders = relativePath.Split('/');

            string AppendPath(int index)
            {
                string buf = "Assets/Resources";
                for(int i = 0; i <= index; i++)
                {
                    buf += "/" + folders[i];
                }
                return buf;
            };

            CreateResourceFolder();

            for(int i = 0; i < folders.Length; i++)
            {
                if (!AssetDatabase.IsValidFolder(AppendPath(i)))
                {
                    AssetDatabase.CreateFolder(AppendPath(i - 1), folders[i]);
                }
            }
        }

        void CreateResourceFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }
#endif
    }

    
}
