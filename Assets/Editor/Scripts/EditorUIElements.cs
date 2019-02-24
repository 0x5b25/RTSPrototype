using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RTS
{
    public abstract class EditorUIBase
    {
        internal virtual Rect position { get; set; } = new Rect(0, 0, 50, 20);
        internal bool relativeSize = false, relativePos = false;
        internal int ctrlID;
        //internal EditorWindow window;

        protected string tag = null;

        public EditorUIBase parent;
        public bool isEnabled = true;

        public virtual void OnConstruct(EditorWindow window) { ctrlID = GetHashCode(); }
        public virtual void OnDisable(EditorWindow window) { }
        public abstract void OnDrawGUI(Rect position, EditorWindow window);

        public virtual void FixInheritance(EditorUIBase parent) { this.parent = parent; }

        public abstract T GetElementByTag<T>(string tag) where T : EditorUIBase;
        public abstract T[] GetAllElementsByTag<T>(string tag) where T : EditorUIBase;
    }

    public abstract class EditorUI<T>:EditorUIBase where T : class
    { 
        public T Width(float w) { position = new Rect(position.position,new Vector2(w,position.height)); return this as T; }
        public T Height(float h) { position = new Rect(position.position, new Vector2(position.width, h)); return this as T; }
        public T RelativeSize(bool isRelative) { this.relativeSize = isRelative; return this as T; }
        public T RelativePosition(bool isRelative) { this.relativePos = isRelative; return this as T; }

        protected bool receiveInput = false;

        protected bool isMouseOver, isMouseDown;
        protected KeyCode keyPressed;

        protected void HandleInput(Rect bounds, EditorWindow window, Action clickCallback = null, Action<Vector2> dragCallback = null)
        {
            isMouseOver = bounds.Contains(Event.current.mousePosition);
            isMouseDown = GUIUtility.hotControl == ctrlID;

            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.KeyDown:
                    {   
                        keyPressed = Event.current.keyCode;
                    }
                    break;
                case EventType.KeyUp:
                    {
                        keyPressed = KeyCode.None;
                    }
                    break;
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                            window.Repaint();
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (isMouseDown && dragCallback != null)
                        {
                            dragCallback(Event.current.delta);
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver || isMouseDown)
                        {
                            GUIUtility.hotControl = 0;
                            if (isMouseOver && isMouseDown)
                                clickCallback?.Invoke();
                            window.Repaint();
                        }
                    }
                    break;
            }
        }

        public string GetTag() { return tag; }
        public T SetTag(string val) { tag = val;return this as T; }

        public override TElem GetElementByTag<TElem>(string tag)
        {
            if (this.tag == tag)
                return this as TElem;
            return null;
        }

        public override TElem[] GetAllElementsByTag<TElem>(string tag)
        {
            var e = GetElementByTag<TElem>(tag);
            if (e != null)
                return new TElem[] { e };
            return null;
        }
    }

    public abstract class EditorUISingleChild<T> : EditorUI<T> where T : class
    {
        public EditorUIBase child;

        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            child?.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            child?.OnDisable(window);
        }

        public bool IsChild(EditorUIBase child) { return child.Equals(this.child); }
        public EditorUIBase GetChild() { return child; }

        public static T operator +(EditorUISingleChild<T> parent, EditorUIBase child) { parent.child = child; child.parent = parent; return parent as T; }
        public override sealed void FixInheritance(EditorUIBase parent) { base.FixInheritance(parent); child?.FixInheritance(this); }

        public override TElem GetElementByTag<TElem>(string tag)
        {
            if (this.tag == tag)
                return this as TElem;
            else if (child != null)
                return child.GetElementByTag<TElem>(tag);
            else return null;
        }

        public override TElem[] GetAllElementsByTag<TElem>(string tag)
        {
            List<TElem> elems = new List<TElem>();
            if(this.tag == tag && this is TElem te)
            {
                elems.Add(te);
            }

            if (child != null)
            {
                var elem = child.GetAllElementsByTag<TElem>(tag);
                if (elem != null)
                    elems.AddRange(elem);
            }
            if (elems.Count == 0)
                return null;
            else return elems.ToArray();
        }
    }

    public abstract class EditorUIMultiChild<T> : EditorUI<T> where T : class
    {
        public List<EditorUIBase> children = new List<EditorUIBase>();

        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            foreach (EditorUIBase ui in children)
                ui?.OnConstruct(window);
        }

        public override void OnDisable(EditorWindow window)
        {
            foreach (EditorUIBase ui in children)
                ui?.OnDisable(window);
        }

        public bool IsChild(EditorUIBase child) { return children.Contains(child); }
        public EditorUIBase GetChild(int index) { if (children.Count > index && index >= 0) return children[index]; return null; }
        public CT GetFirstChild<CT>() where CT : EditorUIBase { return children.Find((EditorUIBase ui) => { return ui is CT; }) as CT; }
        public static T operator +(EditorUIMultiChild<T> parent, EditorUIBase child) { parent.children.Add(child);child.parent = parent; return parent as T; }
        public static T operator -(EditorUIMultiChild<T> parent, EditorUIBase child) { parent.children.Remove(child);child.parent = null; return parent as T; }
        public override sealed void FixInheritance(EditorUIBase parent) { base.FixInheritance(parent); foreach(var child in children)child?.FixInheritance(this); }


        public override TElem GetElementByTag<TElem>(string tag)
        {
            if (this.tag == tag)
                return this as TElem;
            else
            {
                foreach(var elem in children)
                {
                    TElem elemFound = elem.GetElementByTag<TElem>(tag);
                    if (elemFound != null)
                        return elemFound;
                }
            }
            return null;
        }

        public override TElem[] GetAllElementsByTag<TElem>(string tag)
        {
            List<TElem> elems = new List<TElem>();
            if (this.tag == tag && this is TElem te)
                elems.Add(te);

            foreach(var elem in children)
            {
                var elemsFound = elem.GetAllElementsByTag<TElem>(tag);
                if (elemsFound != null)
                    elems.AddRange(elemsFound);
            }

            if (elems.Count == 0)
                return null;
            return elems.ToArray();
        }
    }

    public class ESpacer : EditorUI<ESpacer>
    {
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            
        }
    }

    public class ECascade : EditorUIMultiChild<ECascade>
    {
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            for(int i = 0; i < children.Count; i++)
            {
                children[i].OnDrawGUI(position, window);
            }
        }
    }

    public class EPanel : EditorUIMultiChild<EPanel>
    {
        //public List<EditorUI> children = new List<EditorUI>();

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {

            GUI.BeginClip(position);

            Rect rect = new Rect();
            for (int i = 0; i < children.Count; i++)
            {
                EditorUIBase ui = children[i];
                if (ui.relativePos)
                {
                    rect.position = new Vector2(position.width * ui.position.position.x, position.height * ui.position.position.y);
                }
                else
                {
                    rect.position = ui.position.position;
                }
                if (ui.relativeSize)
                {
                    rect.width = position.width * ui.position.width;
                    rect.height = position.height * ui.position.height;
                }
                else
                {
                    rect.size = ui.position.size;
                }
                ui.OnDrawGUI(rect, window);
            }

            GUI.EndClip();

        }
    }

    public class ESwitchTab : EditorUIMultiChild<ESwitchTab>
    {
        //public List<EditorUIBase> children = new List<EditorUIBase>();
        int activeIndex = 0;
        bool changed = true;
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            GUI.BeginClip(position);
            if (activeIndex >= 0 && activeIndex < children.Count)
                children[activeIndex].OnDrawGUI(new Rect(0,0,position.width,position.height),window);
            GUI.EndClip();
            if (changed)
            {
                window.Repaint();
                changed = false;
            }
        }
        /*public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            foreach (EditorUIBase ui in children)
                ui.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            foreach (EditorUIBase ui in children)
                ui.OnDisable(window);
        }
        #region LayoutOptions
        public ESwitchTab Width(float w) { position.width = w; return this; }
        public ESwitchTab Height(float h) { position.height = h; return this; }
        public ESwitchTab RelativeSize(bool isRelative) { this.relativeSize = isRelative; return this; }
        public ESwitchTab RelativePosition(bool isRelative) { this.relativePos = isRelative; return this; }
        #endregion*/

        #region Children
        /*public bool IsChild(EditorUIBase child) { return children.Contains(child); }
        public EditorUIBase GetChild(int index) { if (children.Count > index && index >= 0) return children[index]; return null; }
        public T GetFirstChild<T>() where T : EditorUIBase { return children.Find((EditorUIBase ui) => { return ui is T; }) as T; }
        public static ESwitchTab operator +(ESwitchTab parent, EditorUIBase child) { parent.children.Add(child); return parent; }
        public static ESwitchTab operator -(ESwitchTab parent, EditorUIBase child) { parent.children.Remove(child); return parent; }
        */
        public static ESwitchTab operator -(ESwitchTab parent, int childIndex)
        {
            if (childIndex >= 0 && childIndex < parent.children.Count)
                parent.children.RemoveAt(childIndex);
            return parent;
        }
        #endregion

        #region Functions
        public ESwitchTab ActivateTab(int index) { this.activeIndex = index;changed = true;cb_ActivateTab?.Invoke(this, index); return this; }
        public int ActivatedTab() { return activeIndex; }
        #endregion

        #region Events
        Action<ESwitchTab, int> cb_ActivateTab;
        public ESwitchTab OnActivateTab(Action<ESwitchTab, int> callback) { cb_ActivateTab = callback; return this; }
        #endregion
    }

    public class EVerticalLayout : EditorUIMultiChild<EVerticalLayout>
    {
        //public List<EditorUIBase> children = new List<EditorUIBase>();
        bool scroll = false;
        bool adaptive = false;
        Vector2 scrollPos = new Vector2();
        Rect viewArea = new Rect(0,0,0,0);
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            viewArea.width = position.width;
            float fixedHeight = 0;
            float relativeHeight = 0;
            //Calculate child hetght
            for (int i = 0; i < children.Count; i++)
            {
                EditorUIBase ui = children[i];
                if (ui.relativeSize)
                    relativeHeight += ui.position.height;
                else
                    fixedHeight += ui.position.height;
            }

            if (adaptive)
            {
                var p = this.position;
                p.height = fixedHeight;
                this.position = p;
                relativeHeight = 0;
                GUI.BeginClip(position);
            }
            else if (scroll && fixedHeight >= position.height)
            {
                viewArea.width -= 15;
                if (relativeHeight > 0)
                {
                    relativeHeight = position.height / relativeHeight;
                    viewArea.height = fixedHeight + position.height;
                }
                else
                {
                    viewArea.height = fixedHeight;
                }
                scrollPos = GUI.BeginScrollView(position, scrollPos, viewArea); 
            }
            else
            {
                if(relativeHeight > 0 && position.height > fixedHeight)
                {
                    relativeHeight = (position.height- fixedHeight) / relativeHeight;
                }
                else
                {
                    relativeHeight = 0;
                }
                GUI.BeginClip(position);
            }
            
            Rect drawArea = new Rect(0, 0, viewArea.width, 0);
            for (int i = 0; i < children.Count; i++)
            {
                EditorUIBase ui = children[i];
                if (ui.relativeSize)
                {
                    drawArea.y += drawArea.height;
                    drawArea.height = relativeHeight * ui.position.height;
                    ui.OnDrawGUI(drawArea,window);
                }
                else
                {
                    drawArea.y += drawArea.height;
                    drawArea.height = ui.position.height;
                    ui.OnDrawGUI(drawArea,window);
                }
            }
            if(adaptive)
                GUI.EndClip();
            else if(scroll && fixedHeight >= position.height)
                GUI.EndScrollView();
            else
                GUI.EndClip();
        }
        /*public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            foreach (EditorUIBase ui in children)
                ui.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            foreach (EditorUIBase ui in children)
                ui.OnDisable(window);
        }*/
        #region LayoutOptions
        /* public EVerticalLayout Width(float w) { position.width = w; return this; }
        public EVerticalLayout Height(float h) { position.height = h; return this; }
        public EVerticalLayout RelativeSize(bool isRelative) { this.relativeSize = isRelative; return this; }
        public EVerticalLayout RelativePosition(bool isRelative) { this.relativePos = isRelative; return this; }*/
        public EVerticalLayout EnableScroll(bool enable) { scroll = enable; return this; }
        public EVerticalLayout AdaptiveHeight(bool enable) { adaptive = enable; return this; }
        #endregion

        #region Children
        /*public bool IsChild(EditorUIBase child) { return children.Contains(child); }
        public EditorUIBase GetChild(int index) { if (children.Count > index && index >= 0) return children[index]; return null; }
        public T GetFirstChild<T>() where T : EditorUIBase { return children.Find((EditorUIBase ui) => { return ui is T; }) as T; }
        public static EVerticalLayout operator +(EVerticalLayout parent, EditorUIBase child) { parent.children.Add(child); return parent; }
        public static EVerticalLayout operator -(EVerticalLayout parent, EditorUIBase child) { parent.children.Remove(child); return parent; }
        */
        #endregion
    }

    public class EVerticalSplitView : EditorUI<EVerticalSplitView>
    {
        protected EditorUIBase uchild, lchild;
        float uHeight = 1, lHeight = 1, handleWidth = 8;
        bool uRelativeHeight = true, lRelativeHeight = true;
        Rect viewArea = new Rect(0, 0, 0, 0);
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if (uRelativeHeight)
            {
                if (lRelativeHeight)
                {
                    float tWidth = uHeight + lHeight;
                    if (tWidth == 0)
                    {
                        uHeight = lHeight = (position.height - handleWidth) / 2;
                    }
                    else
                    {
                        tWidth = (position.height - handleWidth) / tWidth;
                        uHeight = tWidth * uHeight;
                        lHeight = tWidth * lHeight;
                    }
                }
                else
                {
                    uHeight = position.height - handleWidth - lHeight;
                }
            }
            else
            {
                if (lRelativeHeight)
                {
                    lHeight = position.height - handleWidth - uHeight;
                }
            }
            //
            GUI.BeginClip(position);
            viewArea.y = 0;
            viewArea.width = position.width;
            viewArea.height = uHeight;
            uchild?.OnDrawGUI(viewArea, window);
            //GUI.Box(viewArea, "adsgnfaogfroan");
            viewArea.y += uHeight;
            viewArea.height = handleWidth;
            GUI.Box(viewArea, GUIContent.none);
            EditorGUIUtility.AddCursorRect(viewArea, MouseCursor.ResizeVertical);
            GetMouseControl(viewArea, position, window);
            viewArea.y += handleWidth;
            viewArea.height = lHeight;
            lchild?.OnDrawGUI(viewArea, window);
            //GUI.Box(viewArea, "adsgnfaogfroan");
            GUI.EndClip();
        }
        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            uchild?.OnConstruct(window);
            lchild?.OnConstruct(window);
            GUIUtility.GetControlID(FocusType.Passive);
        }
        public override void OnDisable(EditorWindow window)
        {
            uchild?.OnDisable(window);
            lchild?.OnDisable(window);
        }
        #region LayoutOptions
        /*public EVerticalSplitView Width(float w) { position.width = w; return this; }
        public EVerticalSplitView Height(float h) { position.height = h; return this; }
        public EVerticalSplitView RelativeSize(bool isRelative) { this.relativeSize = isRelative; return this; }
        public EVerticalSplitView RelativePosition(bool isRelative) { this.relativePos = isRelative; return this; }
        */public EVerticalSplitView UCellRelativeSize(bool isRelative) { this.uRelativeHeight = isRelative; return this; }
        public EVerticalSplitView LCellRelativeSize(bool isRelative) { this.lRelativeHeight = isRelative; return this; }
        public EVerticalSplitView UCellWidth(float w) { this.uHeight = w; return this; }
        public EVerticalSplitView LCellWidth(float w) { this.lHeight = w; return this; }
        public EVerticalSplitView HandleSize(float s) { this.handleWidth = s; return this; }
        #endregion

        #region Children
        public bool IsChild(EditorUIBase child)
        {
            if (uchild != null)
            {
                if (uchild.Equals(child))
                    return true;
            }
            else if (lchild != null)
            {
                if (lchild.Equals(child))
                    return true;
            }
            return false;
        }
        public EditorUIBase GetUChild(int index) { return uchild; }
        public EditorUIBase GetLChild(int index) { return lchild; }
        public EVerticalSplitView RemoveUChild(int index) { uchild = null; return this; }
        public EVerticalSplitView RemoveLChild(int index) { lchild = null; return this; }
        public static EVerticalSplitView operator +(EVerticalSplitView parent, EditorUIBase child)
        {
            if (parent.uchild == null)
            {
                parent.uchild = child;
            }
            else
            {
                parent.lchild = child;
            }
            child.parent = parent;
            return parent;
        }
        public static EVerticalSplitView operator -(EVerticalSplitView parent, EditorUIBase child)
        {
            if (parent.uchild != null)
            {
                if (parent.uchild.Equals(child))
                {
                    parent.uchild = null;
                    child.parent = null;
                }
            }
            else if (parent.lchild != null)
            {
                if (parent.lchild.Equals(child))
                {
                    parent.lchild = null;
                    child.parent = null;
                }
            }
            return parent;
        }
        #endregion

        void GetMouseControl(Rect bounds, Rect position, EditorWindow window)
        {
            
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;

            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (isDown)
                        {
                            if (Event.current.delta.y < -uHeight)
                            {
                                lHeight += uHeight;
                                uHeight = 0;
                            }
                            else if (Event.current.delta.y > lHeight)
                            {
                                uHeight += lHeight;
                                lHeight = 0;
                            }
                            else
                            {
                                uHeight += Event.current.delta.y;
                                lHeight -= Event.current.delta.y;
                            }
                            window.Repaint();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                        }
                    }
                    break;
            }
        }

        public override TElem GetElementByTag<TElem>(string tag)
        {
            {
                var e = base.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }

            {
                var e = lchild?.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }

            {
                var e = uchild?.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }
            return null;
        }

        public override TElem[] GetAllElementsByTag<TElem>(string tag)
        {
            List<TElem> elems = new List<TElem>();
            if (this.tag == tag && this is TElem te)
                elems.Add(te);
            if (uchild != null)
            {
                var ue = uchild.GetAllElementsByTag<TElem>(tag);
                if (ue != null)
                    elems.AddRange(ue);
            }

            if (lchild != null)
            {
                var le = lchild.GetAllElementsByTag<TElem>(tag);
                if (le != null)
                    elems.AddRange(le);
            }
            return elems.Count > 0 ? elems.ToArray() : null;
        }
    }

    public class EHorizontalLayout : EditorUIMultiChild<EHorizontalLayout>
    {
        bool scroll = false;
        bool adaptive = false;
        Vector2 scrollPos = new Vector2();
        Rect viewArea = new Rect(0, 0, 0, 0);
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            viewArea.height = position.height;
            float fixedWidth = 0;
            float relativeWidth = 0;
            for (int i = 0; i < children.Count;i++)
            {
                EditorUIBase ui = children[i];
                if (ui.relativeSize)
                    relativeWidth += ui.position.width;
                else
                    fixedWidth += ui.position.width;
            }
            if (adaptive)
            {
                var p = this.position;
                p.width = fixedWidth;
                this.position = p;
                relativeWidth = 0;
                GUI.BeginClip(position);
            }
            else if (scroll&& fixedWidth >= position.width)
            {
                viewArea.height -= 15;
                if (relativeWidth > 0)
                {
                    relativeWidth = position.width / relativeWidth;
                    viewArea.width = fixedWidth + position.width;
                }
                else viewArea.width = fixedWidth;
                scrollPos = GUI.BeginScrollView(position, scrollPos, viewArea);
            }
            else
            {
                if (relativeWidth > 0 && position.width > fixedWidth)
                {
                    relativeWidth = (position.width - fixedWidth) / relativeWidth;
                }
                else
                {
                    relativeWidth = 0;
                }
                GUI.BeginClip(position);
            }

            Rect drawArea = new Rect(0, 0, 0, viewArea.height);
            for (int i = 0; i < children.Count; i++)
            {
                EditorUIBase ui = children[i];
                if (ui.relativeSize)
                {
                    drawArea.x += drawArea.width;
                    drawArea.width = relativeWidth * ui.position.width;
                    ui.OnDrawGUI(drawArea,window);
                }
                else
                {
                    drawArea.x += drawArea.width;
                    drawArea.width = ui.position.width;
                    ui.OnDrawGUI(drawArea,window);
                }
            }
            if(adaptive)
                GUI.EndClip();
            else if (scroll && fixedWidth >= position.width)
                GUI.EndScrollView();
            else
                GUI.EndClip();
        }
 
        #region LayoutOptions

        public EHorizontalLayout EnableScroll(bool enable) { scroll = enable; return this; }
        public EHorizontalLayout AdaptiveWidth(bool enable) { adaptive = enable; return this; }
        #endregion

    }

    public class EHorizontalSplitView : EditorUI<EHorizontalSplitView>
    {
        protected EditorUIBase lchild, rchild;
        float lWidth = 1, rWidth = 1, handleWidth = 8;
        bool lRelativeWidth = true, rRelativeWidth = true;
        Rect viewArea = new Rect(0, 0, 0, 0);
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if (lRelativeWidth)
            {
                if(rRelativeWidth)
                {
                    float tWidth = lWidth + rWidth;
                    if(tWidth == 0)
                    {
                        lWidth = rWidth = (position.width - handleWidth) / 2 ;
                    }
                    else
                    {
                        tWidth = (position.width - handleWidth) / tWidth;
                        lWidth = tWidth * lWidth;
                        rWidth = tWidth * rWidth;
                    }
                }
                else
                {
                    lWidth = position.width - handleWidth - rWidth;
                }
            }
            else
            {
                if (rRelativeWidth)
                {
                    rWidth = position.width - handleWidth - lWidth;
                }
            }
            //
            GUI.BeginClip(position);
            viewArea.x = 0;
            viewArea.height = position.height;
            viewArea.width = lWidth;
            lchild?.OnDrawGUI(viewArea, window);
            //GUI.Box(viewArea, "adsgnfaogfroan");
            viewArea.x += lWidth;
            viewArea.width = handleWidth;
            GUI.Box(viewArea, GUIContent.none);
            EditorGUIUtility.AddCursorRect(viewArea, MouseCursor.ResizeHorizontal);
            GetMouseControl(viewArea, window);
            viewArea.x += handleWidth;
            viewArea.width = rWidth;
            rchild?.OnDrawGUI(viewArea, window);
            //GUI.Box(viewArea, "adsgnfaogfroan");
            GUI.EndClip();
        }
        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            lchild?.OnConstruct(window);
            rchild?.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            lchild?.OnDisable(window);
            rchild?.OnDisable(window);
        }
        #region LayoutOptions
        public EHorizontalSplitView LCellRelativeSize(bool isRelative) { this.lRelativeWidth = isRelative; return this; }
        public EHorizontalSplitView RCellRelativeSize(bool isRelative) { this.rRelativeWidth = isRelative; return this; }
        public EHorizontalSplitView LCellWidth(float w) { this.lWidth = w; return this; }
        public EHorizontalSplitView RCellWidth(float w) { this.rWidth = w; return this; }
        public EHorizontalSplitView HandleSize(float s) { this.handleWidth = s; return this; }
        #endregion

        #region Children
        public bool IsChild(EditorUIBase child)
        {
            if (lchild != null)
            {
                if (lchild.Equals(child))
                    return true;
            }
            else if (rchild != null)
            {
                if (rchild.Equals(child))
                    return true;
            }
            return false;
        }
        public EditorUIBase GetLChild(int index) { return lchild; }
        public EditorUIBase GetRChild(int index) { return rchild; }
        public EHorizontalSplitView RemoveLChild(int index) { lchild = null; return this; }
        public EHorizontalSplitView RemoveRChild(int index) { rchild = null; return this; }
        public static EHorizontalSplitView operator +(EHorizontalSplitView parent, EditorUIBase child)
        {
            if (parent.lchild == null)
            {
                parent.lchild = child; 
            }
            else
            {
                parent.rchild = child;
            }
            child.parent = null;
            return parent;
        }
        public static EHorizontalSplitView operator -(EHorizontalSplitView parent, EditorUIBase child)
        {
            if (parent.lchild != null)
            {
                if (parent.lchild.Equals(child))
                {
                    parent.lchild = null;
                    child.parent = null;
                }
            }
            else if(parent.rchild != null)
            {
                if (parent.rchild.Equals(child))
                {
                    parent.rchild = null;
                    child.parent = null;
                }
            }
            return parent;
        }
        #endregion

        void GetMouseControl(Rect bounds, EditorWindow window)
        {

            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;

            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (isDown)
                        {
                            if(Event.current.delta.x < -lWidth)
                            {
                                rWidth += lWidth;
                                lWidth = 0;
                            }
                            else if(Event.current.delta.x > rWidth)
                            {
                                lWidth += rWidth;
                                rWidth = 0;
                            }
                            else
                            {
                                lWidth += Event.current.delta.x;
                                rWidth -= Event.current.delta.x;
                            }
                            window.Repaint();
                        }
                        
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                        }
                    }
                    break;
            }
        }

        public override TElem GetElementByTag<TElem>(string tag)
        {
            {
                var e = base.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }

            {
                var e = lchild?.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }

            {
                var e = rchild?.GetElementByTag<TElem>(tag);

                if (e != null) return e;
            }
            return null;
        }

        public override TElem[] GetAllElementsByTag<TElem>(string tag)
        {
            List<TElem> elems = new List<TElem>();
            if (this.tag == tag && this is TElem te)
                elems.Add(te);
            if (lchild != null)
            {
                var le = lchild.GetAllElementsByTag<TElem>(tag);
                if (le != null)
                    elems.AddRange(le);
            }

            if (rchild != null)
            {
                var re = rchild.GetAllElementsByTag<TElem>(tag);
                if (re != null)
                    elems.AddRange(re);
            }
            return elems.Count > 0 ? elems.ToArray() : null;
        }
    }

    public class EWindowView : EditorUI<EWindowView>
    {
        EditorUIBase child;
        float hHandleWidth = 4, vHandleWidth = 4;
        bool isCollapsed = false;
        bool movable = true;
        bool collapsable = true;
        bool hResizable = true;
        bool vResizable = true;
        Rect collapsedSize = new Rect(0,0,16,200);
        GUIContent title = GUIContent.none;
        bool adaptive = true;
        Rect viewArea = new Rect(0, 0, 0, 0);
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            //Calculate child size if adaptive
            if (adaptive && child != null && !child.relativeSize)
            {
                var p = this.position;
                p.width = child.position.width;
                p.height = child.position.height;
                this.position = p;
            }
            //Draw title
            if(Event.current.type == EventType.Repaint)
            {
                viewArea.position = Vector2.zero;
                viewArea.height = collapsedSize.height;
            }
            //Draw handle
            //Draw content
            GUI.BeginClip(position);

            GUI.EndClip();
        }
        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            child?.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            child?.OnDisable(window);
        }
        #region LayoutOptions

        public EWindowView HResizable(bool enable) { adaptive = enable; return this; }
        public EWindowView VResizable(bool enable) { adaptive = enable; return this; }
        public EWindowView Movable(bool enable) { adaptive = enable; return this; }
        public EWindowView Collapsable(bool enable) { adaptive = enable; return this; }
        //Horizontal resize handle width
        public EWindowView HHandleSize(float s) { this.hHandleWidth = s; return this; }
        //Vertical resize handle height
        public EWindowView VHandleSize(float s) { this.vHandleWidth = s; return this; }
        //Titile height, also collapsed height
        public EWindowView TitleHeight(float s) { this.collapsedSize.height = s; return this; }
        //collapsed Width
        public EWindowView TitleWidth(float s) { this.collapsedSize.width = s; return this; }
        //Enable adaptive size, will disable resize function
        public EWindowView AdaptiveSize(bool enable) { adaptive = enable; return this; }
        #endregion

        #region Children
        public bool IsChild(EditorUIBase child)
        {
            if (child != null)
            {
                if (child.Equals(child))
                    return true;
            }
            return false;
        }
        public EditorUIBase GetChild() { return child; }
        public static EWindowView operator +(EWindowView parent, EditorUIBase child) { parent.child = child; return parent; }
        public static EWindowView operator -(EWindowView parent, EditorUIBase child) { parent.child = null; return parent; }
        #endregion

        Vector2 MouseDelta(Rect bounds, EditorWindow window)
        {
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;

            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (isDown)
                        {
                            window.Repaint();
                            return Event.current.delta;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                        }
                    }
                    break;
            }
            return Vector2.zero;
        }
        bool MousePressed(Rect bounds, EditorWindow window)
        {
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;

            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
    }
    public class EText : EditorUI<EText>
    {
        string content;
        Func<string> updateContent;

        public EText(string content)
        {
            this.content = content;
        }

        public EText() { }

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if (updateContent != null)
                content = updateContent();
            GUI.Label(position, content);
        }


        #region Function
        public EText Content(string content) { this.content = content; return this; }
        public EText BindContent(Func<string> update) { this.updateContent = update; return this; }
        #endregion
    }

    public class EButton : EditorUISingleChild<EButton>
    {
        //public Action callback;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            GUIStyle btnStyle = GUI.skin.FindStyle("button");

            if (Event.current.type == EventType.Repaint)
            {
                btnStyle.Draw(position, isMouseDown || !isEnabled, isMouseDown || !isEnabled, false, false);
            }

            if (child != null)
            {
                child.OnDrawGUI(position, window);
            }
            //bool pressed = GoodButton(position, GUIContent.none,window);
            HandleInput(position, window, clickCallback:()=>
            {
                if (isMouseOver && isEnabled && cb_Clicked != null)
                {
                    cb_Clicked(this);
                }
            });

            

        }
/*
        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            child.OnConstruct(window);
        }
        public override void OnDisable(EditorWindow window)
        {
            child.OnDisable(window);
        }*/


        #region Function
        //public EButton OnClicked(Action callback) { this.callback += callback; return this; }
        #endregion

        #region Events
        Action<EButton> cb_Clicked;
        public EButton OnClicked(Action<EButton> callback) { cb_Clicked = callback; return this; }
        #endregion
        bool GoodButton(Rect bounds, GUIContent caption, EditorWindow window)
        {
            

            GUIStyle btnStyle = GUI.skin.FindStyle("button");
            
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;
            
            if (Event.current.type == EventType.Repaint)
            {
                btnStyle.Draw(bounds, isDown || !isEnabled, isDown ||!isEnabled, false, false);      
            }
            else
                switch (Event.current.type)//GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        {
                            if (isMouseOver)
                            {  // (note: isMouseOver will be false when another control is hot)
                                GUIUtility.hotControl = ctrlID;
                                window.Repaint();//Force update window
                            }
                        }
                        break;

                    case EventType.MouseUp:
                        {
                            if (isMouseOver && isDown)
                            {
                                GUIUtility.hotControl = 0;
                                window.Repaint();
                                return true && isEnabled;   //Block control even if disabled
                            }
                        }
                        break;

                    case EventType.MouseDrag: window.Repaint(); break;
                }
            return false;
        }
    }

    //Multi-line input box
    public class ETextInputArea : EditorUI<ETextInputArea>
    {
        string content;
        Action<string> inputUpdated;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            content = GUI.TextArea(position, content);
            inputUpdated?.Invoke(content);
            //Debug.Log(content);
        }

        #region Function
        public ETextInputArea Content(string content) { this.content = content; return this; }
        public ETextInputArea Callback(Action<string> update) { this.inputUpdated = update; return this; }
        #endregion
    }

    //Single-line input box
    public class ETextInputField : EditorUI<ETextInputField>
    {
        string content;
        Action<ETextInputField,string> cb_OnInputUpdate;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            content = GUI.TextField(position, content);
            cb_OnInputUpdate?.Invoke(this,content);
            //Debug.Log(content);
        }

        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public ETextInputField Content(string content) { this.content = content; return this; }
        public ETextInputField OnInputUpdate(Action<ETextInputField,string> update) { this.cb_OnInputUpdate = update; return this; }
        #endregion
    }

    public class EToggle : EditorUI<EToggle>
    {
        GUIContent content = GUIContent.none;
        bool value;
        Action<bool> callback;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            bool tvalue = GUI.Toggle(position, value, content);
            if(value != tvalue)
            {
                callback?.Invoke(tvalue);
            }
            value = tvalue;
        }

        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public EToggle Content(GUIContent content) { this.content = content; return this; }
        public EToggle Callback(Action<bool> callback) { this.callback = callback; return this; }
        #endregion
    }

    public class EBox : EditorUI<EBox>
    {
        GUIContent content = GUIContent.none;
        Func<GUIContent> updateContent;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if(updateContent != null)
            {
                content = updateContent();
            }
            GUI.Box(position, content);
        }

        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public EBox Content(GUIContent content) { this.content = content; return this; }
        public EBox BindContent(Func<GUIContent> update) { this.updateContent = update; return this; }
        #endregion
    }

    public class ETexture : EditorUI<ETexture>
    {
        Texture2D content;
        ScaleMode scaleMode = UnityEngine.ScaleMode.ScaleToFit;
        Func<Texture2D> updateContent;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if (updateContent != null)
            {
                content = updateContent();
            }
            if (content != null)
                GUI.DrawTexture(position, content, scaleMode);
        }
        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public ETexture Content(Texture2D content) { this.content = content; return this; }
        public ETexture ScaleMode(ScaleMode mode) { this.scaleMode = mode; return this; }
        public ETexture BindContent(Func<Texture2D> update) { this.updateContent = update; return this; }
        #endregion
    }

    public class EPopup : EditorUI<EPopup>
    {
        string[] contents = new string[] { "Empty"};
        int selected = 0;
        Action<EPopup,int> cb_OnSelectionChange;
        Func<string[]> updateContent;

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if(updateContent != null)
            {
                contents = updateContent();
                if(contents == null)
                {
                    contents = new string[] { "Empty" };
                    selected = 0;
                }
            }
            int s = EditorGUI.Popup(position, selected, contents);
            if(s != selected)
            {
                cb_OnSelectionChange?.Invoke(this,s);
                selected = s;
            }
        }
        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public EPopup Content(string[] content) { this.contents = content; return this; }
        public string GetString(int index) { if (index >= 0 && index < contents.Length) return contents[index];return null; }
        public EPopup OnSelectionChange(Action<EPopup,int> callback) { this.cb_OnSelectionChange = callback; return this; }
        public EPopup BindContent(Func<string[]> update) { this.updateContent = update; return this; }
        #endregion
    }

    public class EObjectField<T> : EditorUI<EObjectField<T>> where T : UnityEngine.Object
    {
        T selected = null;
        bool allowSceneObject = false;
        Func<T,bool> callback;

        public EObjectField(){
            Height(16);
            
        }

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            GetHotControl(position, window);
            selected = EditorGUI.ObjectField(position, selected, typeof(T), allowSceneObject) as T;

            if (callback != null)
            {
                if (!callback(selected))
                {
                    selected = null;
                }
            }
        }

        public override void OnDisable(EditorWindow window)
        {

        }

        #region Function
        public EObjectField<T> AllowSceneObject(bool allow) { this.allowSceneObject = allow; return this; }
        public EObjectField<T> Callback(Func<T,bool> callback) { this.callback = callback; return this; }
        #endregion

        bool GetHotControl(Rect bounds, EditorWindow window)
        {
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;


            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;

                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
    }

    public class EPreviewRenderer : EditorUI<EPreviewRenderer>
    {
        public class PreviewModel
        {
            public Mesh mesh;
            public Material mat;
            public Vector3 position;
            public Quaternion rotation;
        }

        public List<PreviewModel> models;

        PreviewRenderUtility util;
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            if (position.width <= 1 || position.height <= 1)
                return;
            GetMouseControl(position,window);
            util.BeginPreview(position,"Box");
            if (models != null)
            {
                foreach (var m in models)
                {
                    if (m.mesh != null && m.mat != null)
                        util.DrawMesh(m.mesh, m.position,m.rotation, m.mat, 0);
                }
            }
            util.camera.Render();
            
            util.EndAndDrawPreview(position);
            
        }
        public override void OnConstruct(EditorWindow window)
        {
            base.OnConstruct(window);
            util = new PreviewRenderUtility();

            //util.camera.fieldOfView = 20f;
            util.camera.farClipPlane = 20f;
            util.camera.nearClipPlane = 0.1f;
            util.camera.transform.position = new Vector3(0, 0, -5);
            //util.camera.transform.rotation = Quaternion.Euler(0, 0, 0);
            //util.camera.clearFlags = CameraClearFlags.Depth;
            //util.camera.backgroundColor = Color.blue;
            //m = CreateBox();
        }
        public override void OnDisable(EditorWindow window)
        {
            util.Cleanup();
            util = null;
        }

        #region Functions
        public EPreviewRenderer SetModels(List<PreviewModel> models) { this.models = models; return this; }
        
        #endregion

        float yaw = 0, pitch = 0, dist = 5;
        void Zoom(float distance)
        {
            dist += distance / 10;
            if (dist < 0.1)
                dist = 0.1f;
        }
        void Pitch(float degree)
        {
            degree /= 90;
            pitch -= degree;
            if (pitch + 0.025 > Mathf.PI / 2)
                pitch = Mathf.PI / 2 - 0.025f;
            else if (pitch - 0.025 < -Mathf.PI / 2)
                pitch = -Mathf.PI / 2 + 0.025f;
        }
        void Yaw(float degree)
        {
            degree /= 90;
            degree -= ((int)(degree / Mathf.PI)) * Mathf.PI;
            yaw += degree;
            if (yaw > Mathf.PI)
                yaw -= 2 * Mathf.PI;
            else if (yaw < -Mathf.PI)
                yaw += 2 * Mathf.PI;
        }
        void SetCamPos()
        {
            if (util == null)
                return;
            float h = -dist * Mathf.Sin(pitch);
            float m = -dist * Mathf.Cos(pitch);
            float x = m * Mathf.Sin(yaw);
            float z = m * Mathf.Cos(yaw);
            util.camera.transform.position = new Vector3(x, h, z);
            util.camera.transform.LookAt(Vector3.zero);
        }

        KeyCode keyPressed;
        void GetMouseControl(Rect bounds, EditorWindow window)
        {
            bool isMouseOver = bounds.Contains(Event.current.mousePosition);
            bool isDown = GUIUtility.hotControl == ctrlID;


            switch (Event.current.type)//GetTypeForControl(controlID))
            {
                case EventType.KeyDown:
                    {
                        keyPressed = Event.current.keyCode;
                    }
                    break;
                case EventType.KeyUp:
                    {
                        keyPressed = KeyCode.None;
                    }
                    break;
                case EventType.MouseDown:
                    {
                        if (isMouseOver)
                        {  // (note: isMouseOver will be false when another control is hot)
                            GUIUtility.hotControl = ctrlID;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (isDown)
                        {
                            
                            if (keyPressed == KeyCode.LeftAlt)
                            {
                                //Zoom
                                Zoom(Event.current.delta.x + Event.current.delta.y);
                                
                            }
                            else
                            {
                                //rotate
                                Yaw(Event.current.delta.x);
                                Pitch(Event.current.delta.y);
                            }
                            SetCamPos();
                            window.Repaint();
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (isMouseOver && isDown)
                        {
                            GUIUtility.hotControl = 0;
                        }
                    }
                    break;
            }
        }

        Mesh CreateBox()
        {
            Mesh mesh = new Mesh();
            //mesh.Clear();

            float length = 1f;
            float width = 1f;
            float height = 1f;

            #region Vertices
            Vector3 p0 = new Vector3(-length * .5f, -width * .5f, height * .5f);
            Vector3 p1 = new Vector3(length * .5f, -width * .5f, height * .5f);
            Vector3 p2 = new Vector3(length * .5f, -width * .5f, -height * .5f);
            Vector3 p3 = new Vector3(-length * .5f, -width * .5f, -height * .5f);

            Vector3 p4 = new Vector3(-length * .5f, width * .5f, height * .5f);
            Vector3 p5 = new Vector3(length * .5f, width * .5f, height * .5f);
            Vector3 p6 = new Vector3(length * .5f, width * .5f, -height * .5f);
            Vector3 p7 = new Vector3(-length * .5f, width * .5f, -height * .5f);

            Vector3[] vertices = new Vector3[]
            {
	// Bottom
	p0, p1, p2, p3,
 
	// Left
	p7, p4, p0, p3,
 
	// Front
	p4, p5, p1, p0,
 
	// Back
	p6, p7, p3, p2,
 
	// Right
	p5, p6, p2, p1,
 
	// Top
	p7, p6, p5, p4
            };
            #endregion

            #region Normales
            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;
            Vector3 front = Vector3.forward;
            Vector3 back = Vector3.back;
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;

            Vector3[] normales = new Vector3[]
            {
	// Bottom
	down, down, down, down,
 
	// Left
	left, left, left, left,
 
	// Front
	front, front, front, front,
 
	// Back
	back, back, back, back,
 
	// Right
	right, right, right, right,
 
	// Top
	up, up, up, up
            };
            #endregion

            #region UVs
            Vector2 _00 = new Vector2(0f, 0f);
            Vector2 _10 = new Vector2(1f, 0f);
            Vector2 _01 = new Vector2(0f, 1f);
            Vector2 _11 = new Vector2(1f, 1f);

            Vector2[] uvs = new Vector2[]
            {
	// Bottom
	_11, _01, _00, _10,
 
	// Left
	_11, _01, _00, _10,
 
	// Front
	_11, _01, _00, _10,
 
	// Back
	_11, _01, _00, _10,
 
	// Right
	_11, _01, _00, _10,
 
	// Top
	_11, _01, _00, _10,
            };
            #endregion

            #region Triangles
            int[] triangles = new int[]
            {
	// Bottom
	3, 1, 0,
    3, 2, 1,			
 
	// Left
	3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
    3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
 
	// Front
	3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
    3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
 
	// Back
	3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
    3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
 
	// Right
	3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
    3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
 
	// Top
	3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
    3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,

            };
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            return mesh;
            //mesh.Optimize();
        }
    }

    public class EToolBar : EditorUI<EToolBar>
    {
        GUIContent[] contents = new GUIContent[] {GUIContent.none };
        Action<int> callback;
        int selected = 0;
        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            int t = GUI.Toolbar(position,selected, contents);
            if (t != selected)
            {
                callback?.Invoke(t);
                selected = t;
            }
        }
        public override void OnDisable(EditorWindow window)
        {
        }

        #region Function
        public EToolBar CallBack(Action<int> callback) { this.callback = callback; return this; }
        public EToolBar Content(GUIContent[] contents) { this.contents = contents; return this; }
        #endregion
    }

    public class EObjectPropertyEditor : EditorUI<EObjectPropertyEditor>
    {
        Rect _position;
        internal override Rect position {
            get{
                if (adaptive)
                {
                    var p = new Rect(_position);
                    p.height = CalculateHeight(objToEdit);
                    return p;
                }
                else
                    return _position;
            }

            set { _position = value; } }

        float fieldHeight = 16;
        bool adaptive = true;
        object objToEdit;
        //System.Reflection.FieldInfo[] props;
        public EObjectPropertyEditor(object o)
        {
            objToEdit = o;
            //props = o.GetType().GetFields();
        }

        public EObjectPropertyEditor()
        {

        }

        public override void OnDrawGUI(Rect position, EditorWindow window)
        {
            Rect drawArea = new Rect();
            drawArea.height = fieldHeight;

            float ind = 10;

            List<object> displayedObjs = new List<object>();

            void DrawEditor(int indentation, object o)
            {
                if (o == null) return;
                if (displayedObjs.Contains(o)) return;
                displayedObjs.Add(o);

                //calculation
                float indCell = indentation * ind;
                float labelCell = position.width / 2 - indCell;
                float editorCell = position.width / 2;

                var fieldInfo = o.GetType().GetFields();
                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    object fieldValue = fieldInfo[i].GetValue(o);

                    //Draw field name
                    drawArea.x = indCell;
                    drawArea.width = labelCell;
                    EditorGUI.LabelField(drawArea, fieldInfo[i].Name);

                    //Set up value editor draw rect
                    drawArea.x += labelCell;
                    drawArea.width = editorCell;

                    if (fieldInfo[i].IsLiteral || fieldInfo[i].IsInitOnly || fieldValue == null)
                    {
                        //Display readonly
                        if(fieldValue != null)
                        {
                            EditorGUI.LabelField(drawArea, fieldValue.ToString());
                        }
                    }
                    else
                    {
                        //Display editor

                        var editor = GetFieldEditor(fieldValue.GetType());
                        if (editor != null)
                        {
                            object newVal = editor.Invoke(drawArea, fieldValue);

                            fieldInfo[i].SetValue(o, newVal);
                        }
                        else
                        {
                            //Reset drawArea position
                            drawArea.y += fieldHeight;
                            DrawEditor(indentation + 1, fieldValue);
                            continue;
                        }

                    }

                    //Reset drawArea position
                    drawArea.y += fieldHeight;
                }
            }

            GUI.BeginClip(position);
            DrawEditor(0, objToEdit);
            GUI.EndClip();
        }

        #region Layout
        public EObjectPropertyEditor AdaptiveHeight(bool enableAdapt) { adaptive = enableAdapt; return this; }
        public EObjectPropertyEditor FieldHeight(float h) { fieldHeight = h; return this; }
        #endregion

        public EObjectPropertyEditor BindObject(object o) { objToEdit = o; return this; }

        float CalculateHeight(object obj)
        {
            List<object> visitedObjs = new List<object>();
            visitedObjs.Add(obj);
            //int basicFieldNum = 0;

            int CountFieldNum(object o)
            {
                if (o == null) return 0;
                
                var fields = o.GetType().GetFields();
                int count = fields.Length;
                foreach (var item in fields)
                {
                    if (GetFieldEditor(item.FieldType) == null)
                    {
                        object subObj = item.GetValue(o);
                        if (!visitedObjs.Contains(subObj))
                        {
                            visitedObjs.Add(subObj);
                            count += CountFieldNum(subObj);
                        }
                    }
                    else
                    {
                        //count++;
                    }
                }

                return count;
            }

            return CountFieldNum(obj) * fieldHeight;
        }

        Func<Rect,object,object> GetFieldEditor(Type fieldType)
        {
            if (fieldType == null) return null;

            if(fieldType.IsSubclassOf(typeof(GameObject)))
            {
                return (Rect r, object b) =>
                {
                    return EditorGUI.ObjectField(r,b as UnityEngine.Object,fieldType,false );
                };
            }
            if(fieldType == typeof( Bounds))
                return (Rect r, object b) => {
                    return EditorGUI.BoundsField(r, (Bounds)b);
                };
            if (fieldType == typeof(BoundsInt))
                return (Rect r, object b) =>
                {
                   return EditorGUI.BoundsIntField(r, (BoundsInt)b);
                };
            if (fieldType == typeof(Color))
                return (Rect r, object b) =>
                {
                    return EditorGUI.ColorField(r, (Color)b);
                };
            if (fieldType == typeof(AnimationCurve))
                return (Rect r, object b) =>
                {
                    return EditorGUI.CurveField(r, (AnimationCurve)b);
                };
            if (fieldType == typeof(double))
                return (Rect r, object b) =>
                {
                    return EditorGUI.DoubleField(r, (double)b);
                };
            if (fieldType.BaseType == typeof(Enum))
                return (Rect r, object b) =>
                {
                    return EditorGUI.EnumPopup(r, (Enum)b);
                };
            if (fieldType == typeof(float))
                return (Rect r, object b) =>
                {
                    return EditorGUI.FloatField(r, (float)b);
                };
            if (fieldType == typeof(Gradient))
                return (Rect r, object b) =>
                {
                    return EditorGUI.GradientField(r, (Gradient)b);
                };
            if (fieldType == typeof(int))
                return (Rect r, object b) =>
                {
                    return EditorGUI.IntField(r, (int)b);
                };
            /*if (t == typeof(BoundsInt))
                return (Rect r, object b) =>
                {
                    return EditorGUI.LayerField(r, (AnimationCurve)b);
                };*/
            if (fieldType == typeof(long))
                return (Rect r, object b) =>
                {
                    return EditorGUI.LongField(r, (long)b);
                };
            /*if (t == typeof(BoundsInt))
                return (Rect r, object b) =>
                {
                   return EditorGUI.MaskField(r, (AnimationCurve)b);
                };*/
            if (fieldType == typeof(Rect))
                return (Rect r, object b) =>
                {
                   return EditorGUI.RectField(r, (Rect)b);
                };
            if (fieldType == typeof(RectInt))
                return (Rect r, object b) =>
                {
                   return EditorGUI.RectIntField(r, (RectInt)b);
                };
            /*if (t == typeof(string))
                return (Rect r, object b) =>
                {
                   return EditorGUI.TagField(r, (string)b);
                };*/
            if (fieldType == typeof(string))
                return (Rect r, object b) =>
                {
                   return EditorGUI.TextField(r, (string)b);
                };
            if (fieldType == typeof(Vector2))
                return (Rect r, object b) =>
                {
                   return EditorGUI.Vector2Field(r,"", (Vector2)b);
                };
            if (fieldType == typeof(Vector2Int))
                return (Rect r, object b) =>
                {
                   return EditorGUI.Vector2IntField(r,"", (Vector2Int)b);
                };
            if (fieldType == typeof(Vector3))
                return (Rect r, object b) =>
                {
                   return EditorGUI.Vector3Field(r,"", (Vector3)b);
                };
            if (fieldType == typeof(Vector3Int))
                return (Rect r, object b) =>
                {
                   return EditorGUI.Vector3IntField(r, "",(Vector3Int)b);
                };
            if (fieldType == typeof(Vector4))
                return (Rect r, object b) =>
                {
                   return EditorGUI.Vector4Field(r,"", (Vector4)b);
                };

            return null;
        
            
        }
    }
}
