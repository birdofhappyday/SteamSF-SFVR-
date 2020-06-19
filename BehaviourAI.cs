using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AI;
using System;

public enum PatrolType
{
    Surround,
    TransformArray,
}

//PJY 17.01.17
public enum EnemyType
{
    None,
    Assault,
    Sniper,
    Shield,
    Rocket,
    Medic,
    BugBug,
    Drone,
    Citizen,
    RedAssault,
}

//PJY 17.03.02
public enum EnemyRed
{
    None,
    Red,
}

[System.Serializable]
public class AIData
{
    public float speed;
    public float defaultSpeed;
    public float approachSpeed;
    public float sight;
    public float sightAngle;
    public float idleTime;
    public float defaultIdleTime;
    public float animationSpeedMove;
    public int   random;
    [HideInInspector]   public int   wave;
    [HideInInspector]   public PatrolType   patrolType;
    [HideInInspector]   public Vector2      patrolRange;
    //[HideInInspector]   public iTweenPath   transformArrayPath = new iTweenPath();
    [HideInInspector]
    public iTweenPath transformArrayPath = null;
    [HideInInspector]   public Transform    target;
    [HideInInspector]   public NavMeshAgent agent;
    [HideInInspector]   public int skillcount;
    [HideInInspector]   public TeamFlag     priority_target_team = TeamFlag.None;
    //[HideInInspector]   public PathData[] AutoPaths;
    //[HideInInspector]   public PathData CurPath;
}

public class BehaviourAI : CharacterBehaviour , IEventBossDead
{
    private CoroutineCommand m_coroutineCommand;
    private Animator         m_animator;
    public  delegate void    ItemEvent();
    public  ItemEvent        itemevent;
    public  AIData           m_data = new AIData();
    public  bool             HpShow;
    public  GameObject       hpBar;
    public GameObject citizenUi;
    public  Image            hp_point;
    public GameObject        Head;
    public GameObject[]      Body;
    public ParticleSystem    Blood_Burst;
    public Transform         breakBodyPos;
    public List<FX> BloodFXList = new List<FX>();
    //PJY 17.01.17
    public EnemyType         enemyType;
    public EnemyRed          enemyRed;
    public string[]          BreakName;
    [HideInInspector] public Rigidbody[] m_rigidbodys;
    [HideInInspector] public Ragdoll m_ragdoll;

    private void OnEnable()
    {
        EventManager.Instance.AddReceiver<IEventBossDead>(this);

        if (Body != null)
        {
            for (int i = 0; i < Body.Length; i++)
            {
                Body[i].SetActive(true);
            }
        }
        if (Head != null)
        {
            Head.SetActive(true);
        }
    }

    public void BossDead(Character character)
    {
        OnDeath();
    }

    public override void OnStart()
    {
        /* // Enemy character add collider : 지혜 01.05
         CapsuleCollider Capcol = gameObject.AddComponent<CapsuleCollider>();
         Capcol.isTrigger = true;*/

        if (hpBar != null)
        {
            hpBar.SetActive(false);
        }

        if(citizenUi != null)
        {
            citizenUi.SetActive(true);
        }

        if (hp_point != null)
        {
            hp_point.fillAmount = 1;
        }
        m_data.agent = GetComponent<NavMeshAgent>();
        m_data.agent.enabled = false;
        m_data.target = null;
        Character.AnimatorAction.Initialize<AnimatorActionCharacter>();

        if (Character.Team == TeamFlag.Enemy)
        {
            if (EnemyType.None != enemyType)
            {
                Character.m_actorUIData.show = UIManager.Instance.Open("UIEnemyShow", true, false, enemyType) as UIEnemyShow;
                Character.m_actorUIData.show.TargetInfo = Character.m_actorUIData.showPos;
            }

            //PJY 17.01.05 UIEnemyShow
            //CoroutineManager.Instance.Register(Delay());

            if (enemyRed == EnemyRed.Red) // 위험한적 Red UI(UIEnemyCircleHP_Red) 표시
            {
                //Character.m_actorUIData.c_show = UIManager.Instance.Open("UIEnemyCircleHP_Red", true) as UIEnemyCircleHP;
                //Character.m_actorUIData.c_show.TargetInfo = Character.m_actorUIData.c_hpPos;
            }

            m_rigidbodys = Character.GetComponentsInChildren<Rigidbody>();

            for (int i = 0; i < m_rigidbodys.Length; i++)
            {
                m_rigidbodys[i].useGravity = true;
                m_rigidbodys[i].isKinematic = true;
            }

            m_animator = Character.GetComponent<Animator>();

            if (!m_animator.isActiveAndEnabled)
                m_animator.enabled = true;

            m_ragdoll = Character.GetComponent<Ragdoll>();

            if (GetComponent<DissolveShader>() != null)
            {
                DissolveShader shader = GetComponent<DissolveShader>();
                shader.Init();
            }

        }
        else if(Character.Team == TeamFlag.Hostage)
        {

            m_rigidbodys = Character.GetComponentsInChildren<Rigidbody>();

            for (int i = 0; i < m_rigidbodys.Length; i++)
            {
                m_rigidbodys[i].useGravity = true;
                m_rigidbodys[i].isKinematic = true;
            }

            m_animator = Character.GetComponent<Animator>();

            if (!m_animator.isActiveAndEnabled)
                m_animator.enabled = true;

            m_ragdoll = Character.GetComponent<Ragdoll>();

            if (GetComponent<DissolveShader>() != null)
            {
                DissolveShader shader = GetComponent<DissolveShader>();
                shader.Init();
            }


           // return;
            //Character.m_actorUIData.show = UIManager.Instance.Open("UIEnemyShow", true, false, enemyType) as UIEnemyShow;
            //Character.m_actorUIData.show.TargetInfo = Character.m_actorUIData.showPos;
        }
    }

    public override void OnAttacked(DamageData damageData)
    {
        if (Character.Team == TeamFlag.Enemy)
        {
            if (hp_point != null)
            {
                hp_point.fillAmount = (float)Character.Status.HP / (float)Character.Status.MaxHP;
            }
            if (damageData != null && m_data.target != null && damageData.Attacker != null)
            {
                m_data.target = damageData.Attacker.transform;
            }
            //if (HpShow == true)
            //{
            //    CoroutineManager.Instance.Register(Delay());
            //}
            Character.AnimatorAction.Action(AnimatorState.Attacked);
        }
        //PJY 12.29 
        //UIManager.Instance.OpenDamageText(Character.m_actorUIData.hpPos, damageData.ResultDamage.ToString());
    }

    public override void OnDeath(DamageData damagedata = null)
    {
        if (hpBar != null)
        {
            hpBar.SetActive(false);
        }

        if(citizenUi != null)
        {
            citizenUi.SetActive(false);
        }

        if (null != Character.m_actorUIData.c_show)
        {
            UIManager.Instance.Close(Character.m_actorUIData.c_show);
            Character.m_actorUIData.c_show = null;
        }

        if (null != Character.m_actorUIData.show)
        {
            UIManager.Instance.Close(Character.m_actorUIData.show);
            Character.m_actorUIData.show = null;
        }

        //string enemyScore = "50";
        //UIManager.Instance.OpenScoreText(Character.m_actorUIData.hpPos, enemyScore);

        DropItem item = gameObject.GetComponent<DropItem>() as DropItem;

        if (item != null)
        {
            item.Process();
        }
      
        m_data.agent.enabled = false;

        Explosion explosionOrigin = gameObject.GetComponent<Explosion>() as Explosion;

        if (explosionOrigin != null)
        {
            explosionOrigin.Process();
        }


        if (damagedata != null)
        {
            if (damagedata.AttackType == AttackType.Splash)
            {
                if (m_ragdoll != null)
                {
                    m_ragdoll.PlayRagdoll();
                    HitBox hit = this.GetComponentsInChildren<HitBox>()[0];// 성민 추가
                    if (hit != null)
                    {
                        (InGameManager.Instance.Player.CurrentRightWeapon as GunBaseRight).Enemy_Break(hit, BreakName, BodyPart.Torso);
                    }
                    m_coroutineCommand = CoroutineManager.Instance.Register(DeadTimer());
                }
                else
                {

                    Character.AnimatorAction.Action(AnimatorState.Dead);
                }
            }
            else
            {
                //Character.AnimatorAction.Action(AnimatorState.Dead);
                if (m_ragdoll != null)
                {
                    m_ragdoll.PlayRagdoll();
                    //HitBox hit = this.GetComponentsInChildren<HitBox>()[0];// 성민 추가
                    //if (hit != null)
                    //{
                    //    (InGameManager.Instance.Player.CurrentRightWeapon as GunBaseRight).Enemy_Break(hit, BreakName, BodyPart.Torso);

                    //}
                    m_coroutineCommand = CoroutineManager.Instance.Register(DeadTimer());
                }
                else
                {
                    Character.AnimatorAction.Action(AnimatorState.Dead);
                }
            }
        }
        else
        {
            if (m_ragdoll != null)
            {
                m_ragdoll.PlayRagdoll();
                //HitBox hit = this.GetComponentsInChildren<HitBox>()[0];// 성민 추가
                //if (hit != null)
                //{
                //    (InGameManager.Instance.Player.CurrentRightWeapon as GunBaseRight).Enemy_Break(hit, BreakName, BodyPart.Torso);

                //}
                m_coroutineCommand = CoroutineManager.Instance.Register(DeadTimer());
            }
            else
            {
                Character.AnimatorAction.Action(AnimatorState.Dead);
            }
     
        }


        if (Character.Team == TeamFlag.Hostage)
        {
            InGameManager.Instance.InGameData.TotalScore -= 3000;
            if (0 > InGameManager.Instance.InGameData.TotalScore )
            {
                InGameManager.Instance.InGameData.TotalScore = 0;
                InGameManager.Instance.InGameData.WaveScore = 0;
            }
        }
        else
        {
            InGameManager.Instance.InGameData.TotalScore += 50;
            InGameManager.Instance.InGameData.WaveScore += 50;
        }
    }

    public override void OnRestore()
    {
        EventManager.Instance.RemoveReceiver<IEventBossDead>(this);

        if (Character.Team == TeamFlag.Enemy)
        {   
            if (null != Character.m_actorUIData.c_show)
            {
                UIManager.Instance.Close(Character.m_actorUIData.c_show);
                Character.m_actorUIData.c_show = null;
            }
                
            if (null != Character.m_actorUIData.show)
            {
                UIManager.Instance.Close(Character.m_actorUIData.show);
                Character.m_actorUIData.show = null;
            }
            for(int i = 0; i < BloodFXList.Count; i++)
            {
                BloodFXList[i].Restore();
                BloodFXList.RemoveAt(i);
            }


            CoroutineManager.Instance.Unregister(m_coroutineCommand);
            
        }

        if(Character.Team == TeamFlag.Hostage)
        {
            return;
            if (null != Character.m_actorUIData.c_show)
            {
                UIManager.Instance.Close(Character.m_actorUIData.c_show);
                Character.m_actorUIData.c_show = null;
            }

            if (null != Character.m_actorUIData.show)
            {
                UIManager.Instance.Close(Character.m_actorUIData.show);
                Character.m_actorUIData.show = null;
            }

            CoroutineManager.Instance.Unregister(m_coroutineCommand);
        }
    }

    private void Update()
    {
        if (hpBar != null)
        {
            hpBar.transform.LookAt(Camera.main.transform);
        }
    }

    //PJY 17.01.17 생성후 1초뒤 UI Open
    public IEnumerator<CoroutinePhase> Delay()
    {
        if (this.GetComponent<Character>() != null && this.GetComponent<Character>().IsAlive())
        {
            hpBar.SetActive(true);
        }

        yield return Suspend.Do(3.0f);
        hpBar.SetActive(false);
    }


    private IEnumerator<CoroutinePhase> DeadTimer()
    {
        const float WAITTIME = 3f;

        yield return Suspend.Do(WAITTIME);

        if (GetComponent<DissolveShader>() != null)
        {
            DissolveShader shader = GetComponent<DissolveShader>();
            shader.Process();
        }

        yield return Suspend.Do(WAITTIME);
        Character.Restore();
    }

}
