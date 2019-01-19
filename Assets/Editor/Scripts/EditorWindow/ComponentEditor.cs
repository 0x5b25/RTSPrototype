using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

namespace RTS.Editor
{
    public class ComponentEditor : EditorWindow
    {

        static ComponentEditor window;

        [MenuItem("Window/RTS/ComponentEditor")]
        public static void InitWindow()
        {
            window = GetWindow<ComponentEditor>();
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
                    tabs = new ESwitchTab().RelativeSize(true)
                    + new DBEditTab(this);

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
    }

    class DBEditTab:EVerticalLayout
    {
        internal ComponentEditor parent;

        EVerticalLayout dbPageList = new EVerticalLayout().EnableScroll(true).RelativeSize(true);

        EButton addPageButton, delPageButton, editPageButton;

        AssetTypes.ComponentDB db;

        void UpdatePageList()
        {
            EditorUIBase GenPageEntry(int index)
            {
                var disp = new EHorizontalLayout()
                            + new EText().Content(index.ToString()).Width(20)
                            + new EText().BindContent(() =>
                            {
                                if (db != null && db.pages.Count > index)
                                    return db.pages[index].pageName;
                                else return "OUT OF SCOPE!";
                            }).RelativeSize(true);

                var tabs = new ESwitchTab();
                var element = tabs
                    //Unselected
                    + (new EButton().Callback(() => { SelectPageEntry(index); })
                        + disp
                        )
                    //Selected
                    + (new ECascade()
                            + disp
                            + new EBox()
                            )
                    //Rename
                    + (new EHorizontalLayout()
                            + new ETextInputField().Content(db.pages[index].pageName).Callback((val)=> { db.pages[index].pageName = val; }).RelativeSize(true)
                            + (new EButton().Width(50).Callback(()=> { tabs.ActivateTab(1); }) + new EText("done"))
                            )
                    ;

                element.OnConstruct(parent);
                return element;
            }

            if(dbPageList.children.Count < db.pages.Count)
            {
                for(int i = dbPageList.children.Count; i < db.pages.Count; i++)
                {
                    dbPageList.children.Add(GenPageEntry(i));
                }
            }else if(db.pages.Count < dbPageList.children.Count)
            {
                dbPageList.children.RemoveRange(db.pages.Count, dbPageList.children.Count - db.pages.Count);
            }

            for (int i = 0; i < dbPageList.children.Count; i++)
            {
                ESwitchTab entry = (dbPageList.children[i] as ESwitchTab);
                if (parent.selectedPage == i)
                {
                    if (entry.ActivatedTab() == 0)
                        entry.ActivateTab(1);
                }
                else entry.ActivateTab(0);
            }

            if (parent.selectedPage == -1)
            {
                delPageButton.isEnabled = false;
                editPageButton.isEnabled = false;
            }
            else
            {
                delPageButton.isEnabled = true;
                editPageButton.isEnabled = true;
            }
        }

        void SelectPageEntry(int index)
        {
            for(int i = 0; i < dbPageList.children.Count; i++)
            {
                (dbPageList.children[i] as ESwitchTab).ActivateTab(index == i ? 1 : 0);
            }
            parent.selectedPage = index;
        }

        public DBEditTab(ComponentEditor p)
        {
            parent = p;
            db = parent.database;
            //title bar
            children.Add(new EText().Height(16).Content("Select page"));
            //content
            addPageButton = new EButton().RelativeSize(true).Callback(AddNewPage) + new EText().Content("Add");
            delPageButton = new EButton().RelativeSize(true).Callback(DelSelectedPage) + new EText().Content("Del");
            editPageButton = new EButton().RelativeSize(true).Callback(EditSelectedPage) + new EText().Content("Rename");

            children.Add(new EHorizontalSplitView().RelativeSize(true).LCellRelativeSize(false).LCellWidth(200) 
                + (new EVerticalLayout()
                    + (new EHorizontalLayout().Height(16)
                        +addPageButton
                        +delPageButton
                        +editPageButton
                        )
                    + new ESpacer().Height(8)
                    + dbPageList
                    )
                );
            //operation bar
            children.Add(new EButton());
        }

        private void EditSelectedPage()
        {
            (dbPageList.children[parent.selectedPage] as ESwitchTab).ActivateTab(2);
        }

        private void DelSelectedPage()
        {
            db.pages.RemoveAt(parent.selectedPage);
        }

        private void AddNewPage()
        {
            db.pages.Add(new AssetTypes.ComponentDBPage());
            UpdatePageList();
            SelectPageEntry(db.pages.Count - 1);
            EditSelectedPage();
        }

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            UpdatePageList();
            base.OnDrawGUI(position, window);
        }
    }
}
