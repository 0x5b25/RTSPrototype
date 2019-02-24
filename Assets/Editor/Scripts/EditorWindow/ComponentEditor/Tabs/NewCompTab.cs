using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTS.AssetTypes;
using UnityEditor;
using UnityEngine;
using static RTS.AssetTypes.RTSComponentInfo;

namespace RTS.Editor.ComponentEditor.Tabs
{
    class NewCompTab:EVerticalLayout
    {
        ComponentEditorWindow window;

        ETextInputField
            aimingAngleField,
            horizontalRotationSpeedField,
            verticalRotationSpeedField;

        public NewCompTab(ComponentEditorWindow window)
        {
            RelativeSize(true);
            this.window = window;

            aimingAngleField = new ETextInputField().OnInputUpdate((ETextInputField f, string val) =>
            {
                compAim.aimingAngle = Convert.ToSingle(val);
            }).RelativeSize(true);
            horizontalRotationSpeedField = new ETextInputField().OnInputUpdate((ETextInputField f, string val) =>
            {
                compAim.horizontalRotationSpeed = Convert.ToSingle(val);
            }).RelativeSize(true);
            verticalRotationSpeedField = new ETextInputField().OnInputUpdate((ETextInputField f, string val) =>
            {
                compAim.verticalRotationSpeed = Convert.ToSingle(val);
            }).RelativeSize(true);

            List<string> aimingTypes = new List<string>(Enum.GetNames(typeof(ComponentAimingType)));
            aimingTypes.Insert(0, "Disable aiming");

            var aimConfig = new ESwitchTab().RelativeSize(true).OnActivateTab((ESwitchTab tab, int i) => { })
                + new EBox().Content(new GUIContent("Aiming disabled"))
                + (new EVerticalLayout()
                    + (new EHorizontalLayout()
                        +(new EText("Aiming angle resolution").RelativeSize(true))
                        + aimingAngleField
                        )
                    + (new EHorizontalLayout()
                        + (new EText("Horizontal rotation speed").RelativeSize(true))
                        + horizontalRotationSpeedField
                        )
                    + (new EHorizontalLayout()
                        + (new EText("Vertical rotation speed").RelativeSize(true))
                        + verticalRotationSpeedField
                        )
                    );

            children.Add(new EVerticalLayout().RelativeSize(true)
            //Name input field
                
                //Name viability status
                
                
                +(new EHorizontalLayout()
                    //Component type
                    + (new ETextInputField().OnInputUpdate((ETextInputField f, string val) => SetName(val))).RelativeSize(true).Width(3)
                    + new EPopup()  .RelativeSize(true).Width(1)
                                    .Content(Enum.GetNames(typeof(ComponentType)))
                                    .OnSelectionChange((EPopup p,int i)=> { SetType(i); })
                    )
                + (new EText().BindContent(() => { return CheckNameViability() ? "All good" : "!! Naming conflict !!"; }))
                     //Component aiming mechanism
                + new EPopup()
                                    .Content(aimingTypes.ToArray())
                                    .OnSelectionChange((EPopup p, int i) => {
                                        if (i == 0)
                                        {
                                            aimConfig.ActivateTab(0);
                                        }
                                        else
                                        {
                                            aimConfig.ActivateTab(1);
                                        }
                                        SetAimType(i);
                                    })
                + aimConfig
                );

            children.Add(
                new EHorizontalLayout()
                + (new EButton().RelativeSize(true).OnClicked((EButton b)=>CreateComponent()) + new EText("Create"))
                + (new EButton().RelativeSize(true).OnClicked((EButton b) => window.OpenDBEditTab()) + new EText("Cancel"))
                );
        }

        ComponentType compType;
        RTSComponentAimingData compAim;
        
        string compName;

        bool CheckNameViability()
        {
            if (compName == null || compName == "")
                return false;
            var db = window.GetSelectedPage();
            if (db == null)
                return false;
            foreach (var item in db.componentList)
            {
                if (item.name == compName)
                    return false;
            }
            return true;
        }

        void SetName(string name)
        {
            compName = name;
        }

        void SetType(int index)
        {
            compType = (ComponentType)index;
        }

        void SetAimType(int index)
        {
            if(index == 0)
            {
                compAim = null;
            }
            else
            {
                if (compAim == null)
                    compAim = new RTSComponentAimingData();
                compAim.aimingType = (ComponentAimingType)index - 1;
                UpdateAimData();
            }
        }

        void UpdateAimData()
        {
            aimingAngleField.Content(compAim.aimingAngle.ToString());
            horizontalRotationSpeedField.Content(compAim.horizontalRotationSpeed.ToString());
            verticalRotationSpeedField.Content(compAim.verticalRotationSpeed.ToString());
        }

        void CreateComponent()
        {
            if (!CheckNameViability())
                return;

            var comp = new RTSComponentInfo();
            comp.name = compName;
            comp.type = compType;
            comp.aimingData = compAim;

            string path = "RTSComponents/" + window.GetSelectedPage().pageName + '/' + compName;
            //PrefabUtility.
            RTS.Subsys.DataProvider.CreateResourceFolder(path);
            GameObject go = new GameObject("CompTemp", typeof(Unit));
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/" + path + "/prefab.prefab");
            GameObject.DestroyImmediate(go,false);

            window.selectedComp = window.GetSelectedPage().componentList.Count;
            window.GetSelectedPage().componentList.Add(comp);
            window.OpenCompEditTab();
        }


    }
}
