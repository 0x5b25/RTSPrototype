using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace RTS.Editor.ComponentEditor.Tabs
{
    class CompEditTab:EVerticalLayout
    {

        ComponentEditorWindow editorWindow;

        public CompEditTab(RTS.Editor.ComponentEditorWindow window)
        {
            editorWindow = window;
            var comp = window.GetSelectedComp();
            children.Add(new EObjectPropertyEditor().AdaptiveHeight(true).SetTag("objEdit"));
        }

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            GetElementByTag<EObjectPropertyEditor>("objEdit").BindObject(editorWindow.GetSelectedComp());
            base.OnDrawGUI(position, window);
        }
    }
}
