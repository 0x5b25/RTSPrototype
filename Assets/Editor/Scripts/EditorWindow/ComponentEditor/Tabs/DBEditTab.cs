using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTS.Editor;
using UnityEditor;
using UnityEngine;

namespace RTS.Editor.ComponentEditor.Tabs
{
    class DBEditTab : EVerticalLayout
    {
        internal ComponentEditorWindow window;

        class PageListPanel : EVerticalLayout
        {
            private ComponentEditorWindow window;

            EButton addPageButton;
            EVerticalLayout dbPageList;

            public PageListPanel(ComponentEditorWindow window)
            {
                EnableScroll(true);
                RelativeSize(true);
                addPageButton = new EButton().OnClicked((EButton b) => AddNewPage()).Width(70) + new EText("New page");
                dbPageList = new EVerticalLayout().EnableScroll(true).RelativeSize(true);
                this.window = window;
                this.children.Add(
                    (new EHorizontalLayout().Height(16)
                        + addPageButton
                        )
                    );
                this.children.Add(
                    new ESpacer().Height(8)
                    );
                this.children.Add(dbPageList);
            }

            public override void OnDrawGUI(Rect position, EditorWindow window)
            {
                UpdatePageList();
                base.OnDrawGUI(position, window);
            }

            void UpdatePageList()
            {
                EditorUIBase GenPageEntry(int index)
                {
                    var disp = new EHorizontalLayout()
                                + new EText().Content(index.ToString()).Width(20)
                                + new EText().BindContent(() =>
                                {
                                    if (window.database.pages.Count > index)
                                        return window.database.pages[index].pageName;
                                    else return "OUT OF SCOPE!";
                                }).RelativeSize(true);

                    var tabs = new ESwitchTab();
                    var element = tabs
                        //Unselected
                        + (new EButton().OnClicked((EButton b) => { SelectPageEntry(index); })
                            + disp
                            )
                        //Selected
                        + (new ECascade()
                            + disp
                            + new EBox()
                            )

                        ;

                    element.OnConstruct(window);
                    return element;
                }

                if (dbPageList.children.Count < window.database.pages.Count)
                {
                    for (int i = dbPageList.children.Count; i < window.database.pages.Count; i++)
                    {
                        dbPageList.children.Add(GenPageEntry(i));
                    }
                }
                else if (window.database.pages.Count < dbPageList.children.Count)
                {
                    dbPageList.children.RemoveRange(window.database.pages.Count, dbPageList.children.Count - window.database.pages.Count);
                }

                for (int i = 0; i < dbPageList.children.Count; i++)
                {
                    ESwitchTab entry = (dbPageList.children[i] as ESwitchTab);
                    if (window.selectedPage == i)
                    {
                        if (entry.ActivatedTab() == 0)
                            entry.ActivateTab(1);
                    }
                    else entry.ActivateTab(0);
                }
            }
            void SelectPageEntry(int index)
            {
                window.selectedPage = index;
            }
            void AddNewPage()
            {
                window.database.pages.Add(new AssetTypes.ComponentDBPage());
                UpdatePageList();
                SelectPageEntry(window.database.pages.Count - 1);
            }
        }

        class PageContentPanel : EVerticalSplitView
        {
            private ComponentEditorWindow window;

            EButton delPageButton, editPageButton;

            EVerticalLayout compList;

            ESwitchTab namingBar;

            public PageContentPanel(ComponentEditorWindow window)
            {
                RelativeSize(true);
                UCellRelativeSize(false);
                UCellWidth(200);
                HandleSize(4);
                this.window = window;
                delPageButton = new EButton().Width(100).OnClicked((EButton b) => DelPage()) + new EText().Content("Del");
                editPageButton = new EButton().Width(100).OnClicked((EButton b) => EditPage()) + new EText().Content("Rename");

                namingBar = (new ESwitchTab().Height(16)
                        + (new EHorizontalLayout().Height(16)
                            + new EText().BindContent(() =>
                            {
                                return this.window.selectedPage == -1 ? "" : window.database.pages[this.window.selectedPage].pageName;
                            })
                            .RelativeSize(true)
                            + editPageButton
                            + delPageButton
                            )
                        + (new EHorizontalLayout().Height(16)
                            + new ETextInputField().RelativeSize(true)
                            + editPageButton
                            + delPageButton
                            )
                        );

                compList = new EVerticalLayout().RelativeSize(true).EnableScroll(true);
                uchild = (new EVerticalLayout().RelativeSize(true)
                        + namingBar
                        + (new EButton().Height(20) + new EText("Flags"))
                        + (new EVerticalLayout().RelativeSize(true).EnableScroll(true)
                            + new EBox().Content(new GUIContent("Placeholder")).Height(50)
                            + new EBox().Content(new GUIContent("Placeholder")).Height(50)
                            + new EBox().Content(new GUIContent("Placeholder")).Height(50)
                            )
                    );
                //page contents
                lchild = (
                    new EVerticalLayout().RelativeSize(true)
                    + (new EButton().OnClicked((EButton b)=>window.OpenCompCreateTab()) + new EText("New component"))
                    + compList
                    );

            }

            public override void OnDrawGUI(Rect position, EditorWindow window)
            {
                UpdateCompList();
                base.OnDrawGUI(position, window);
            }

            void UpdateCompList()
            {
                EditorUIBase GenCompEntry(int index)
                {
                    var disp = new EHorizontalLayout()
                                + new EText().Content(index.ToString()).Width(20)
                                + new EText().BindContent(() =>
                                {
                                    AssetTypes.ComponentDBPage p = window.GetSelectedPage();
                                    if (p != null)
                                    {
                                        if (p.componentList.Count > index)
                                            return p.componentList[index].name;
                                    }
                                    return "OUT OF SCOPE!";
                                }).RelativeSize(true);

                    var tabs = new ESwitchTab().Height(100);
                    var element = tabs
                        //Unselected
                        + (new EButton().OnClicked((EButton b) => { window.selectedComp = index; window.OpenCompEditTab(); })
                            + disp
                            )
                        ;

                    element.OnConstruct(window);
                    return element;
                }

                AssetTypes.ComponentDBPage page = window.GetSelectedPage();

                if (page == null)
                {
                    compList.children.Clear();
                    return;
                }

                if (compList.children.Count < page.componentList.Count)
                {
                    for (int i = compList.children.Count; i < page.componentList.Count; i++)
                    {
                        compList.children.Add(GenCompEntry(i));
                    }
                }
                else if (page.componentList.Count < compList.children.Count)
                {
                    compList.children.RemoveRange(page.componentList.Count, compList.children.Count - page.componentList.Count);
                }
            }

            private void EditPage()
            {
                ((namingBar.children[1] as EHorizontalLayout).children[0] as ETextInputField).Content(
                    window.selectedPage == -1 ? "" : window.database.pages[this.window.selectedPage].pageName
                    );
                namingBar.ActivateTab(1);
            }

            private void DelPage()
            {
                window.database.pages.RemoveAt(window.selectedPage);
                window.selectedPage = -1;
            }



        }

        EVerticalLayout dbPageList = new EVerticalLayout().EnableScroll(true).RelativeSize(true);



        public DBEditTab(ComponentEditorWindow window)
        {
            this.window = window;
            //title bar
            children.Add(new EText().Height(16).Content("Select page"));
            //content



            children.Add(new EHorizontalSplitView().RelativeSize(true).LCellRelativeSize(false).LCellWidth(200)
                //page entry list
                + (new PageListPanel(window)
                    )
                //page detail panel
                + (new PageContentPanel(window)
                    )
                );
            //operation bar
            children.Add(new EButton());
        }



        public override void OnDrawGUI(Rect position, EditorWindow window)
        {

            base.OnDrawGUI(position, window);
        }
    }
}
