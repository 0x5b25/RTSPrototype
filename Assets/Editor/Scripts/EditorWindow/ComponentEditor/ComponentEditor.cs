using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace RTS.Editor
{
    public class ComponentEditorWindow : EditorWindow
    {

        static ComponentEditorWindow window;

        [MenuItem("Window/RTS/ComponentEditor")]
        public static void InitWindow()
        {
            window = GetWindow<ComponentEditorWindow>();
            window.titleContent = new GUIContent("Component Editor");
            window.Construct();
            //window.wantsMouseMove = true;
            window.Show();
            //UnityEditor.Editor.
        }

        AssetTypes.ComponentDB db;

        EditorUIBase root;

        ESwitchTab tabs;

        void Construct()
        {
            if (database == null)
            {
                database = RTS.Subsys.DataProvider.Get().componentDatabase;
            }

            if (root == null)
            {
                //Build ui

                if (tabs == null)
                {
                    tabs = new ESwitchTab().RelativeSize(true)
                    + new ComponentEditor.Tabs.DBEditTab(this)
                    + new ComponentEditor.Tabs.NewCompTab(this)
                    + new ComponentEditor.Tabs.CompEditTab(this)
                    ;
                    tabs.FixInheritance(null);
                }

                root = new EVerticalLayout()
                    + tabs;
                
                PrefabStage.prefabStageClosing += Closing;
            }


            root.OnConstruct(this);

        }

        GameObject prefab;
        string prefabPath;
        PrefabStage stage;
        internal RTS.AssetTypes.ComponentDB database;
        internal int selectedPage = -1;
        internal int selectedComp = -1;

        internal AssetTypes.ComponentDBPage GetSelectedPage()
        {
            if (selectedPage < 0 || selectedPage >= database.pages.Count)
                return null;
            return database.pages[selectedPage];
        }
        internal AssetTypes.RTSComponentInfo GetSelectedComp()
        {
            var db = GetSelectedPage();
            if (db == null)
                return null;
            if (selectedComp < 0 || selectedComp >= db.componentList.Count)
                return null;
            return db.componentList[selectedComp];
        }
        #region Tabs
        internal void OpenCompEditTab()
        {
            tabs.ActivateTab(2);
        }

        internal void OpenDBEditTab()
        {
            tabs.ActivateTab(0);
        }

        internal void OpenCompCreateTab()
        {
            if(selectedPage >= 0)
            tabs.ActivateTab(1);
        }

        #endregion


        #region AssetFunc

        bool AssignPrefab(GameObject p)
        {
            if (p != null && PrefabUtility.IsPartOfAnyPrefab(p))
            {
                prefab = p;
                prefabPath = AssetDatabase.GetAssetPath(prefab);
                return true;
            }
            else
            {
                prefab = null;
                prefabPath = String.Empty;
                return false;
            }

        }

        void DisplayPrefab()
        {
            if (prefab == null) return;

            stage = PrefabStageUtility.GetCurrentPrefabStage();

            if(stage == null || !stage.IsPartOfPrefabContents(prefab))
            {
                if (!AssetDatabase.OpenAsset(prefab))
                    throw new Exception("Cant open prefab!");
                stage = PrefabStageUtility.GetCurrentPrefabStage();
            }

            //stage = PrefabStageUtility.GetPrefabStage(prefab);

           
//             if(stage == null || !stage.IsPartOfPrefabContents(prefab))
//             {
//                 throw new Exception("Cant enter prefab stage!");
//             }

            tabs.ActivateTab(1);
        }

        void CheckOpenedPrefab()
        {
            if(stage!= null)
            {
               if(prefabPath == stage.prefabAssetPath)
                {
                    Debug.Log("Same path");
                }
                return;
            }
            Debug.Log("No match");
        }

        void Closing(PrefabStage s)
        {
            if (stage == null) return;
            if(s == stage)
            {
                stage = null;
                tabs.ActivateTab(0);
            }
            Repaint();
        }

        private void OnGUI()
        {
            if(window == null)
            {
                window = this;
            }
           
            root.OnDrawGUI(new Rect(0,0,position.width,position.height),this);          
        }

        private void OnEnable()
        {
            Construct();
        }

        private void OnDisable()
        {
            root.OnDisable(this);
            PrefabStage.prefabStageClosing -= Closing;
        }
        #endregion
    }

    
}
