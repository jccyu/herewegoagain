using System;
using System.Reflection;
using System.Collections.Generic;
using RoR2;
using BepInEx;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates.Engi.FireDrone;
using System.Linq;
using EntityStates.Engi.EngiBubbleShield;
using EntityStates.Commando.CommandoWeapon;

namespace Again
{
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.jccyu.herewegoagain", "Here We Go Again", "0.0.1")]

    public class AgainPlugin : BaseUnityPlugin
    {
        const float shieldLifeTime = 2.5f;
        const int maxTurretPlacable = 5;
        List<DeployableInfo> turretList;

        public void Awake()
        {
            Chat.AddMessage("* " + maxTurretPlacable + " turrets can be placed simutaneously. *");
            Chat.AddMessage("* ALSO FREE FOOONGUSES. *");

            SurvivorAPI.SurvivorCatalogReady += (s, e) =>
            {
                SurvivorDef engineer = SurvivorAPI.SurvivorDefinitions[1];
                GameObject engibody = BodyCatalog.FindBodyPrefab("EngiBody");

                // secondary: a single strike drone
                GenericSkill secondary = engibody.GetComponent<SkillLocator>().secondary;
                secondary.activationState = new EntityStates.SerializableEntityStateType(typeof(FireDrone));
                object secbox = secondary.activationState;
                var secfield = typeof(EntityStates.SerializableEntityStateType)?.
                GetField("_typeName", BindingFlags.NonPublic | BindingFlags.Instance);
                secfield?.SetValue(secbox, typeof(FireDrone)?.AssemblyQualifiedName);
                secondary.activationState = (EntityStates.SerializableEntityStateType) secbox;
                secondary.skillNameToken = "Strike Drone";
                secondary.skillDescriptionToken = "Summons a strike drone for 10 seconds.";
                secondary.icon = Resources.Load<Sprite>("Textures/ItemIcons/texRadioIcon");
                secondary.baseRechargeInterval = 5f;
                secondary.baseMaxStock = 3;

                // utility: quick shield
                GenericSkill utility = engibody.GetComponent<SkillLocator>().utility;
                utility.skillNameToken = "Quick Shield";
                utility.skillDescriptionToken = "Place the shield for " + shieldLifeTime + " seconds.";
                utility.baseRechargeInterval = 6f;

                // special: turret recharge speed and max charges
                engibody.GetComponent<SkillLocator>().special.baseRechargeInterval = 1f;
                engibody.GetComponent<SkillLocator>().special.baseMaxStock = maxTurretPlacable;

                engibody.GetComponent<SkillLocator>();
                engibody.GetComponent<GenericSkill>();
                SurvivorAPI.SurvivorDefinitions.RemoveAt(1);
                SurvivorAPI.SurvivorDefinitions.Insert(2, engineer);

                //// adding bandit
                //SurvivorDef bandit = new SurvivorDef
                //{
                //    bodyPrefab = BodyCatalog.FindBodyPrefab("BanditBody"),
                //    descriptionToken = "Bandito",
                //    displayPrefab = Resources.Load<GameObject>("Prefabs/Characters/BanditDisplay"),
                //    primaryColor = new Color(0.8039216f, 0.482352942f, 0.843137264f),
                //    unlockableName = ""
                //};

                //SurvivorAPI.SurvivorDefinitions.Insert(3, bandit);
            };

            // lowered lifetime for shields
            On.EntityStates.Engi.EngiBubbleShield.Deployed.FixedUpdate += On_FixedUpdate;

            // add infinite deploy for turrets
            On.RoR2.CharacterMaster.AddDeployable += On_AddDeployable;
            On.RoR2.CharacterMaster.RemoveDeployable += On_RemoveDeployable;
            
            On.RoR2.Run.Start += On_Start;
        }

        private void On_FixedUpdate
            (On.EntityStates.Engi.EngiBubbleShield.Deployed.orig_FixedUpdate orig, Deployed self)
        {
            Deployed.lifetime = shieldLifeTime;
            orig.Invoke(self);
        }

        private void On_AddDeployable
            (On.RoR2.CharacterMaster.orig_AddDeployable orig, 
            CharacterMaster self, Deployable deployable, DeployableSlot slot)
        {
            if (slot == DeployableSlot.EngiTurret)
            {
                if (!NetworkServer.active)
                {
                    Debug.LogWarning("[Server] function 'System.Void RoR2.CharacterMaster::AddDeployable(RoR2.Deployable,RoR2.DeployableSlot)' called on client");
                    return;
                }
                if (deployable.ownerMaster)
                    Debug.LogErrorFormat("Attempted to add deployable {0} which already belongs to master {1} to master {2}.", new object[]
                    {
                    deployable.gameObject,
                    deployable.ownerMaster.gameObject,
                    base.gameObject
                    });
                if (this.turretList == null)
                    this.turretList = new List<DeployableInfo>();
                if (turretList.Count == maxTurretPlacable)
                {
                    Deployable temp = turretList[0].deployable;
                    turretList.RemoveAt(0);
                    temp.ownerMaster = null;
                    temp.onUndeploy.Invoke();
                }
                this.turretList.Add(new DeployableInfo
                {
                    deployable = deployable,
                    slot = slot
                });
                deployable.ownerMaster = self;
            }
            else
            {
                orig.Invoke(self, deployable, slot);
            }
        }

        private void On_RemoveDeployable
            (On.RoR2.CharacterMaster.orig_RemoveDeployable orig, 
            CharacterMaster self, Deployable deployable)
        {
            orig.Invoke(self, deployable);
            for (int i = turretList.Count - 1; i >= 0; i--)
                if (turretList[i].deployable == deployable)
                    turretList.RemoveAt(i);
        }

        private void On_Start
            (On.RoR2.Run.orig_Start orig, Run self)
        {
            orig.Invoke(self);
            foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances)
            {
                pcmc.master.inventory.GiveItem(ItemIndex.Mushroom, 5);
                //pcmc.master.inventory.GiveItem(ItemIndex.SprintBonus, 5);
                //pcmc.master.inventory.SetEquipmentIndex(EquipmentIndex.DroneBackup);
            }
        }
    }
}