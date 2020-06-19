using UnityEngine;
using System.Collections.Generic;

//AniIdle 함수에서 Action함수를 반복적으로 호출해서 업데이트 문에서 돕니다.
//그래서 m_isRemove값을 통해서 리스트의 첫번째만 지우도록 했습니다.

public class AnimatorActionBossAdd : AnimatorActionBase
{
    private List<int> m_skillarry = new List<int>();
    private Character m_character = null;

    //리스트의 첫 변수를 지우기 위한 체크입니다.

    private bool m_first = true;

    public AnimatorActionBossAdd(Animator animator, AnimatorModule module) : base(animator, module)
    {
        m_character = animator.GetComponent<Character>();
        Log.Error(null != m_character, "AnimatorActionBoss.cs: Character is non");
    }

    private void Shuffle(List<int> skilldata)
    {
        int skill_count = skilldata.Count;

        for (int i = 0; i < skill_count; i++)
        {
            int random = Random.Range(0, skill_count);
            int temp = skilldata[random];
            skilldata[random] = skilldata[i];
            skilldata[i] = temp;
        }        
    }

    public override void Action(AnimatorState state, params int[] intParams)
    {
        base.Action(state, intParams);
        
            // 리스트가 비워졌을 경우 들어와서 숫자를 채웁니다.
            if (0 == m_skillarry.Count || m_first)
            {
                for (int i = 4; i < m_character.SkillManage.GetSkillNum; ++i)
                {
                    m_skillarry.Add(i + 1);
                }
                Shuffle(m_skillarry);
                m_first = false;
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