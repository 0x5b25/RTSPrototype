using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RTS.Editor
{
    public class EventEditor:EditorWindow
    {
        static EventEditor window;

        [MenuItem("Window/RTS/EventEditor")]
        public static void InitWindow()
        {
            window = GetWindow<EventEditor>();
            window.titleContent = new GUIContent("Event Editor");
            window.Construct();
            window.Show();
        }

        internal RTS.AssetTypes.RTSConfig cfg;
        EVerticalLayout contentList, root;

        void Construct()
        {
            cfg = RTS.Subsys.DataProvider.Get().config;

            if (contentList == null)
                contentList = new EVerticalLayout().RelativeSize(true).EnableScroll(true);

            if (root == null)
            {
                root = new EVerticalLayout();

                root.children.Add(contentList);
                root.children.Add(new EButton().RelativeSize(false).OnClicked((EButton b) => AddNewEvent()).Height(40) + new EText().Content("Add new event"));
            }
            root.OnConstruct(this);
        }

        private void OnGUI()
        {
            if (cfg.events.Count < contentList.children.Count)
                contentList.children.RemoveRange(cfg.events.Count, contentList.children.Count - cfg.events.Count);

            else
                for (int i = contentList.children.Count; i < cfg.events.Count; i++)
                {
                    {
                        contentList += new EventContentLine(i, this).Height(16);
                    }
                }


            root.OnDrawGUI(new Rect(0, 0, position.width, position.height), this);
        }

        private void OnEnable()
        {
            Construct();
        }

        private void OnDisable()
        {
            root.OnDisable(this);
        }

        private void OnLostFocus()
        {
            //Discard unsaved changes
            FinishEdit();
        }

        internal void BeginEdit(int index)
        {
            for(int i = 0; i < contentList.children.Count; i++)
            {
                if (i != index) ((contentList.children[i]) as EventContentLine).DisableEdit();
                else ((contentList.children[i]) as EventContentLine).EnableEdit();
            }
            Repaint();
        }

        internal void FinishEdit()
        {
            for (int i = 0; i < contentList.children.Count; i++)
            {
                ((contentList.children[i]) as EventContentLine).Reset();
            }
            Repaint();
        }

        internal void RemoveEvent(int index)
        {
            cfg.events.RemoveAt(index);
            contentList.children.RemoveAt(contentList.children.Count - 1);

            FinishEdit();
        }

        void AddNewEvent()
        {
            cfg.events.Add("New event");
            contentList += new EventContentLine(cfg.events.Count - 1, this).Height(16);
            BeginEdit(cfg.events.Count - 1);
        }
    }

    class EventContentLine : ESwitchTab
    {
        internal int index;
        EText indexText, nameText/*Used in normal mode*/;
        ETextInputField nameField/*Used in edit mode*/;
        EButton editButton,editFinishButton, deleteButton;

        EventEditor parent;

        public EventContentLine(int index,EventEditor parent)
        {
            this.index = index;
            this.parent = parent;
            indexText = new EText().Content(index.ToString()).RelativeSize(false).Width(20);
            nameField = new ETextInputField().RelativeSize(true).OnInputUpdate((ETextInputField f,string val)=> { parent.cfg.events[this.index] = val;nameText.Content(val); });
            nameText = new EText().Content(parent.cfg.events[index]).RelativeSize(true);

            editButton = new EButton().RelativeSize(false).Width(80).OnClicked((EButton b)=> { parent.BeginEdit(index); }) + new EText().Content("edit");
            editFinishButton = new EButton().RelativeSize(false).Width(50).OnClicked((EButton b) => { parent.FinishEdit(); }) + new EText().Content("finish");
            deleteButton = new EButton().RelativeSize(false).Width(30).OnClicked((EButton b) => { parent.RemoveEvent(index); }) + new EText().Content("x");

            children.Add(new EHorizontalLayout() + indexText + nameText + editButton);
            children.Add(new EHorizontalLayout() + indexText + nameField + editFinishButton + deleteButton);
            children.Add(new EHorizontalLayout() + indexText + nameText);
            this.OnConstruct(parent);
        }

        internal void EnableEdit()
        {
            
            nameField.Content(parent.cfg.events[index]);
            ActivateTab(1);
        }

        internal void Reset()
        {
            nameText.Content(parent.cfg.events[index]);
            ActivateTab(0);
        }

        internal void DisableEdit()
        {
            ActivateTab(2);
        }
    }
}
