using UnityEngine;
using System.Collections.Generic;

//AniIdle 함수에서 Action함수를 반복적으로 호출해서 업데이트 문에서 돕니다.
//그래서 m_isRemove값을 통해서 리스트의 첫번째만 지우도록 했습니다.

public class AnimatorActionBoss : AnimatorActionBase
{
    private List<int>           m_skillarry     = new List<int>();                  
    private Character           m_character     = null;
    private BehaviourBoss       m_behaviour     = null;

    //리스트의 첫 변수를 지우기 위한 체크입니다.

    public AnimatorActionBoss(Animator animator, AnimatorModule module) : base(animator, module)
    {
        m_character = animator.GetComponent<Character>();
        Log.Error(null != m_character, "AnimatorActionBoss.cs: Character is non");

        m_behaviour = animator.GetComponent<BehaviourBoss>();
        Log.Error(null != m_behaviour, "AnimatorActionBoss.cs: BehaviourBoss is non");
    }

    private void Shuffle(List<int> skilldata)
    {
        //int skill_count = m_character.SkillManage.GetSkillNum;

        for (int i = 0; i < skilldata.Count; i++)
        {
            int random          = Random.Range(0, skilldata.Count);
            int temp            = skilldata[random];
            skilldata[random]   = skilldata[i];
            skilldata[i]        = temp;
        }

        if (m_behaviour.islastskill == true)
        {
            //마지막 스킬이 처음에 동작하지 않기 위한 함수입니다.
            if (skilldata.Count == m_skillarry[0])
            {
                int temp = skilldata[0];
                skilldata[0] = skilldata[skilldata.Count / 2];
                skilldata[skilldata.Count / 2] = temp;
            }
        }       
    }

    public override void Action(AnimatorState state, params int[] intParams)
    {
        base.Action(state, intParams);

        int skill_count = 0;
        
            // 리스트가 비워졌을 경우 들어와서 숫자를 채웁니다.
        if (0 == m_skillarry.Count)
        {
            if (m_behaviour.isactiveskill == false)
            {
                skill_count = m_character.SkillManage.GetSkillNum;
            }

            else
            {
                skill_count = 4;
            }


            for (int i = 0; i < skill_count; ++i)
            {
                m_skillarry.Add(i + 1);
            }

            Shuffle(m_skillarry);
        }
      
        switch (state)
        {
            case AnimatorState.Skill:
                m_animator.SetInteger("Skill", m_skillarry[0]);
                if (intParams[0] == -1)
                {
                    m_skillarry.RemoveAt(0);
                }
                break;
        }
    }

    public override void Action(AnimatorState state, params float[] floatParams)
    {
        base.Action(state, floatParams);

        switch (state)
        {
            case AnimatorState.Speed:
                m_animator.SetFloat("Speed", floatParams[0]);
                break;
        }
    }
}