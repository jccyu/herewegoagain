using System;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.FireDrone
{
    internal class FireDrone : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound("Play_item_use_radio", gameObject);
            duration = FireDrone.baseDuration / attackSpeedStat;
            if (base.GetModelAnimator())
            {
                float num = duration * 0.3f;
                PlayCrossfade("Gesture, Additive", "FireMineRight", "FireMine.playbackRate", duration + num, 0.05f);
            }
            if (NetworkServer.active)
            {
                Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * 3f;
                Vector3 position3 = transform.position + new Vector3(vector.x, 0f, vector.y);
                SummonMaster(Resources.Load<GameObject>("Prefabs/CharacterMasters/DroneBackupMaster"), position3)
                    .gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = lifespan + UnityEngine.Random.Range(0f, 3f);
            }
        }

        public override void OnExit() =>
            base.OnExit();

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority() =>
            InterruptPriority.PrioritySkill;

        private float duration = 1f;
        private static float lifespan = 10f;
        private static float baseDuration = 1f;

        private CharacterMaster SummonMaster(GameObject masterObjectPrefab, Vector3 position)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'RoR2.CharacterMaster RoR2.EquipmentSlot::SummonMaster(UnityEngine.GameObject,UnityEngine.Vector3)' called on client");
                return null;
            }
            GameObject gameObject = UnityEngine.Object.Instantiate(masterObjectPrefab, position, base.transform.rotation);
            NetworkServer.Spawn(gameObject);
            CharacterMaster component = gameObject.GetComponent<CharacterMaster>();
            component.SpawnBody(component.bodyPrefab, position, base.transform.rotation);
            AIOwnership component2 = gameObject.GetComponent<AIOwnership>();
            if (component2)
            {
                CharacterBody characterBody = this.characterBody;
                if (characterBody)
                {
                    CharacterMaster master = characterBody.master;
                    if (master)
                    {
                        component2.ownerMaster = master;
                    }
                }
            }
            BaseAI component3 = gameObject.GetComponent<BaseAI>();
            if (component3)
            {
                component3.leader.gameObject = base.gameObject;
            }
            return component;
        }
    }
}
