using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
public enum BossTarget
{
    Down,
    WingL,
    WingR,
    None, 
}

[System.Serializable]
public class StateUpdate
{
    [Range(0, 1)]
    public float hpPercent;
    public GameObject[] activeObject;
    public string[] skillnumber;

    public void ActiveHpEffect()
    {
        for (int i = 0; i < activeObject.Length; ++i)
        {
            activeObject[i].SetActive(true);
        }
    }

    public bool CheckPossiblity(string atext)
    {
        for (int i = 0; i < skillnumber.Length; ++i)
        {
            if (skillnumber[i] == atext)
            {
                return true;
            }
        }

        return false;
    }
}

public class BehaviourBoss : CharacterBehaviour
{
    public AIData               m_data          = new AIData();
    [HideInInspector]
    public Rigidbody[]          m_rigidbodys;

    [Header("Health Options")]
    public bool                 activeHPUpdate  = false;
    public StateUpdate[]        m_hpState;
    public StateUpdate          m_curHpState;

    //public GameObject[]         m_target;
    public float                m_flicktime;
    //타겟폭발로 사용.
    public GameObject[]         DeadExplotion;
    //public GameObject[]         m_colider;

    [Tooltip("true : node autopatrol에서 애니메이션 초기화, false : 그대로 스킬 발동")]
    public bool isactiveskill = false;

    [Tooltip("true : 시작부터 타겟이 깜박입니다. false : node autopatrol에서 실행됩니다.")]
    public bool istarget = false;

    public bool islastskill = false;

    public UIBossHP[] m_bossHpBar;
    //private CoroutineCommand    m_damagetestcorutineCommand;
    private CoroutineCommand    m_bossdelaycorutineCommand = null;
    private CoroutineCommand    m_bosshpcorutineCommand = null;
    //private UIBossShow          m_bossShow;

    private UIBossTarget        m_bossTarget;
    private int                 m_bossTargetpart;
    private int                 m_bossHpCount;
    private int                 m_bosshp;
    private bool                m_first;
    
    /// <summary>
    /// 애니메이션 start됬을때 실행되는 함수. 기본 변수들 세팅과 등장 애니메이션을 실행해준다.
    /// </summary>
    public override void OnStart()
    {
        m_data.agent            = GetComponent<NavMeshAgent>();
        m_data.agent.enabled    = false;
        m_data.target           = null;
        m_bossHpCount           = Character.m_actorUIData.bossHPPos.Length;
        m_bosshp                = CharacterTable.GetProperty(Character.name).hp;
        m_first                 = true;
        m_bossTarget            = GetComponent<UIBossTarget>();

        if (isactiveskill)
        {
            Character.AnimatorAction.Initialize<AnimatorActionDroneBoss>();
        }
        else
        {
            Character.AnimatorAction.Initialize<AnimatorActionBoss>();
        }

        //보스 등장 애니메이션 실행을 위한 세팅.
        Character.AnimatorAction.Action(AnimatorState.Appear, true);

        //보스 약점 부위를 나타내는 변수.
        m_bossTargetpart = 0;

        if (m_hpState.Length > 0)
        {
            m_curHpState = m_hpState[0];
        }

        //m_rigidbodys = transform.GetComponentsInChildren<Rigidbody>();

        //for (int y = 0; y < m_rigidbodys.Length; ++y)
        //{
        //    m_rigidbodys[y].useGravity = true;
        //    m_rigidbodys[y].isKinematic = true;
        //}

        //m_bossTarget.Initialize(m_target, m_colider);
        //m_bossHpBar = new UIBossHP[m_bossHpCount];
        
        //보스의 타겟에 hp를 띄울 장소를 설정합니다.
        for (int i = 0; i < m_bossHpCount; ++i)
        {
            //m_bossHpBar[i] = GetComponent<BossInfoBoss>().bosshpBar;
            m_bossHpBar[i] = UIManager.Instance.Open("UIBossHP", true) as UIBossHP;
            m_bossHpBar[i].TargetInfo = Character.m_actorUIData.bossHPPos[i];
            //hp 갯수 구분을 위해서 보스 이름 설정.
            m_bossHpBar[i].Initialize();
            m_bossHpBar[i].Invisible();
            m_bossHpBar[i].HpSetup(m_bossHpCount, m_bosshp);

            //FX fx = AssetManager.FX.Retrieve("Boss_Target");
            //fx.transform.parent = m_target[i].gameObject.transform;
            //fx.transform.position = m_target[i].transform.FindChild("Boss_Target").position;
        }
        
        m_bosshpcorutineCommand = CoroutineManager.Instance.Register(BosstargetFlicker());
        //m_damagetestcorutineCommand = CoroutineManager.Instance.Register(Damage());
    }

    /// <summary>
    /// 공격당했을 때 실행되는 애니메이션 함수.
    /// </summary>
    /// <param name="damageData"></param>
    public override void OnAttacked(DamageData damageData)
    {
        m_bossHpBar[m_bossTargetpart].Damage(damageData.ResultDamage);

        if (damageData.ResultDamage != 0 && m_bossdelaycorutineCommand == null)
        {
            m_bossdelaycorutineCommand = CoroutineManager.Instance.Register(BossDelay());
        }

        //타겟이 켜져서 공격받는 중에 타겟의 hp가 0이되면 작동합니다.
        //0이 되면 처음 코루틴을 끄고 타겟의 폭발 fx를 실행합니다.
        //다음 순서의 타겟을 켭니다.
        if (m_bossHpBar[m_bossTargetpart].Close())
        {
            if (null != m_bosshpcorutineCommand)
            {
                CoroutineManager.Instance.Unregister(m_bosshpcorutineCommand);
                m_bosshpcorutineCommand = null;
            }

            if (null != m_bossdelaycorutineCommand)
            {
                CoroutineManager.Instance.Unregister(m_bossdelaycorutineCommand);
                m_bossdelaycorutineCommand = null;
            }

            m_bossTarget.InVisibleObj(m_bossTargetpart);
            m_bossHpBar[m_bossTargetpart].Invisible();
            m_bossTarget.VisibleColider(m_bossTargetpart);

            if (null != DeadExplotion[m_bossTargetpart + 1] && !DeadExplotion[m_bossTargetpart + 1].activeSelf)
            {
                DeadExplotion[m_bossTargetpart + 1].SetActive(true);
            }
            
            m_bossTargetpart = HpCheck(m_bossTargetpart);
            
            if (-1 == m_bossTargetpart)
            {
                OnDeath();
            }

            m_bosshpcorutineCommand = CoroutineManager.Instance.Register(BosstargetFlicker());
        }            

        if (damageData != null && m_data.target != null && damageData.Attacker != null)
        {
            m_data.target = damageData.Attacker.transform;
        }
        
        if (activeHPUpdate)
        {
            Update_HpAction(Character.Status.HP, Character.Status.MaxHP);
        }
       
        //UIManager.Instance.OpenDamageText(Character.m_actorUIData.hpPos, damageData.ResultDamage.ToString());
    }

    /// <summary>
    /// 죽었을 때 실행되는 애니메이션 함수.
    /// </summary>
    /// <param name="damagedata"></param>
    public override void OnDeath(DamageData damagedata = null)
    {
        //CoroutineManager.Instance.Unregister(m_damagetestcorutineCommand);

        if(null != m_bosshpcorutineCommand)
        {
            CoroutineManager.Instance.Unregister(m_bosshpcorutineCommand);
            m_bosshpcorutineCommand = null;
        }

        if (m_bossdelaycorutineCommand != null)
        {
            CoroutineManager.Instance.Unregister(m_bossdelaycorutineCommand);
            m_bossdelaycorutineCommand = null;
        }

        string bossScore = "1000";
        UIManager.Instance.OpenScoreText(Character.m_actorUIData.hpPos, bossScore);

        for (int i = 0; i < m_bossHpCount; ++i)
        {
            UIManager.Instance.Close(m_bossHpBar[i]);
            m_bossTarget.InVisibleObj(i);
        }

        //몸체 부분 폭발
        if (null != DeadExplotion[0])
        {
            DeadExplotion[0].SetActive(true);
        }

        EventManager.Instance.Notify<IEventBossDead>((receiver) => receiver.BossDead(Character));
        
        Character.AnimatorAction.Action(AnimatorState.Dead);
    }

    /// <summary>
    /// 개체가 꺼졋을 때 실행되는 함수. 상속받은 개체에서 행동을 결정한다.
    /// </summary>
    public override void OnRestore()
    {
    }

    /// <summary>
    /// Hp UI의 값을 세팅해서 보여준다.
    /// </summary>
    /// <param name="curhp"></param>
    /// <param name="maxhp"></param>
    private void Update_HpAction(float curhp, float maxhp)
    {
        for (int i = 0; i < m_hpState.Length; ++i)
        {
            if (curhp / maxhp < m_hpState[i].hpPercent)
            {
                m_hpState[i].ActiveHpEffect();
                m_curHpState = m_hpState[i];
            }
        }
    }

    //공격 받았을때 hp가 3초간 보이고 사라집니다.
    private IEnumerator<CoroutinePhase> BossDelay()
    {
        m_bossHpBar[m_bossTargetpart].Visable();
        yield return Suspend.Do(3.0f);
        m_bossHpBar[m_bossTargetpart].Invisible();

        if (m_bossdelaycorutineCommand != null)
        {
            CoroutineManager.Instance.Unregister(m_bossdelaycorutineCommand);
            m_bossdelaycorutineCommand = null;
        }
    }
    
    //공격 할 수 있는 타겟이 순차적으로 깜박입니다. 타겟이 공격받을 경우에만 데미지가 들어옵니다.
    private IEnumerator<CoroutinePhase> BosstargetFlicker()
    {
        float time = 0.0f;

        while (m_first)
        {
            if (istarget)
            {   
                if (isactiveskill)
                {
                    Character.AnimatorAction.Initialize<AnimatorActionBossAdd>();
                    isactiveskill = false;
                }

                m_first = false;

                break;
            }
            
            yield return Suspend.OneFrame();
        }

        while (true)
        {
            time += Time.deltaTime;

            m_bossTarget.VisibleObj(m_bossTargetpart);
            m_bossTarget.InVisibleColider(m_bossTargetpart);

            m_bossTarget.VisibleMesh(m_bossTargetpart);
            yield return Suspend.Do(0.2f);

            if(-1 == m_bossTargetpart || m_bossHpBar[m_bossTargetpart].Close())
            {
                time = 0.0f;
                break;
            }

            m_bossTarget.InVisibleMesh(m_bossTargetpart);
            yield return Suspend.Do(0.2f);
           
            if (-1 == m_bossTargetpart || m_bossHpBar[m_bossTargetpart].Close())
            {
                time = 0.0f;
                break;
            }

            if (time > m_flicktime)
            {
                m_bossTarget.InVisibleObj(m_bossTargetpart);
                m_bossHpBar[m_bossTargetpart].Invisible();
                m_bossTarget.VisibleColider(m_bossTargetpart);

                time = 0.0f;
                
                m_bossTargetpart = HpCheck(m_bossTargetpart);
                if (-1 == m_bossTargetpart)
                {
                    break;
                }
            }

            yield return Suspend.OneFrame();
        }        
    }

    /// <summary>
    /// 보스 hp가 0이 아닌 곳을 찾아 반환합니다.
    /// hp전부다 0일 경우 -1읇 반환합니다.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private int HpCheck(int pos)
    {
        for (int i = 0; i < m_bossHpCount; ++i)
        {
            ++pos;

            if (m_bossHpCount <= pos)
            {
                pos = 0;
            }

            if (0.0f < m_bossHpBar[pos].CurHp)
            {
                return pos;
            }

        }

        pos = -1;

        return pos;
    }

    //// 보스에 데미지주는 Test.
    //private IEnumerator<CoroutinePhase> Damage()
    //{
    //    yield return Suspend.Do(1.5f);

    //    while (true)
    //    {
    //        yield return Suspend.OneFrame();

    //        DamageData data = new DamageData(InGameManager.Instance.Player, BodyPart.Torso, 1);
    //        Character.OnAttacked(data);
    //    }
    //}

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.X))
    //    {
    //        EventManager.Instance.Notify<IEventBossDead>((receiver) => receiver.BossDead(Character));
    //    }
    //}
}