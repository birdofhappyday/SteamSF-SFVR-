using UnityEngine;
using System.Collections.Generic;
using System;

public class CharacterManager : SingleTon<CharacterManager>
{
    private List<Character>[] m_teamList;

    private CharacterManager()
    {
        TeamFlag[] teamFlag = (TeamFlag[])System.Enum.GetValues(typeof(TeamFlag));
        m_teamList = new List<Character>[teamFlag.Length];

        for(int i = 0; i < m_teamList.Length; i++)
        {
            m_teamList[i] = new List<Character>();
        }
    }

    public void Initialize()
    {
        for(int i = 0; i < m_teamList.Length; i++)
        {
            m_teamList[i].Clear();
        }
    }

    public void AddCharacter(Character character)
    {
        if(null == character)
        {
            return;
        }

        if(false == character.gameObject.activeSelf)
        {
            return;
        }

        if(TeamFlag.None == character.Team)
        {
            return;
        }

        List<Character> list = m_teamList[character.Team.ToIndex()];
        if(true == list.Contains(character))
        {
            return;
        }

        list.Add(character);
        EventManager.Instance.Notify<IEventCharacterSpawn>((receiver) => receiver.CharacterSpawn(character));
    }

    public void RemoveCharacter(Character character)
    {
        if(null == character)
        {
            return;
        }

        if(false == character.gameObject.activeSelf)
        {
            return;
        }

        if(TeamFlag.None == character.Team)
        {
            return;
        }

        List<Character> list = m_teamList[character.Team.ToIndex()];
        if(false == list.Contains(character))
        {
            return;
        }

        list.Remove(character);
        ProcessAfterDeath(character);
        EventManager.Instance.Notify<IEventCharacterDead>((receiver) => receiver.CharacterDead(character));
    }

    public void RefreshTeamList(TeamFlag team)
    {
        if(TeamFlag.None == team)
        {
            return;
        }

        List<Character> list      = m_teamList[team.ToIndex()];
        List<Character> editList  = new List<Character>();

        for(int i = 0; i < list.Count; i++)
        {
            if(null == list[i])
            {
                editList.Add(list[i]);
                continue;
            }

            if(team != list[i].Team)
            {
                editList.Add(list[i]);
                continue;
            }
        }

        for(int i = 0; i < editList.Count; i++)
        {
            list.Remove(editList[i]);
        }
    }

    public int GetTeamCount(TeamFlag team)
    {
        return m_teamList[team.ToIndex()].Count;
    }

    public Character GetEnemyByRange(Character character, float distance, float angle, bool attack = true)
    {
        //Priority Select Target
        for (int i = 1; i < m_teamList.Length; i++)
        {
            if ((int)character.Team == i)
            {
                continue;
            }
            if (character.Team == TeamFlag.Enemy)
            {
                if (i == TeamManager.ToIndex(character.GetComponent<BehaviourAI>().m_data.priority_target_team))
                {
                    List<Character> enemyList = m_teamList[i];

                    for (int k = 0; k < enemyList.Count; k++)
                    {
                        Character enemy = enemyList[k];

                        Vector3 dir = enemy.transform.position - character.transform.position;
                        if (Vector3.Angle(dir, character.transform.forward) > angle * 0.5f)
                        {
                            continue;
                        }

                        if (dir.magnitude > distance)
                        {
                            continue;
                        }

                        //특수 기능으로 적이 있어도 감지를 못하게 만든다.
                        if (!attack)
                        {
                            continue;
                        }
                        return enemy;
                    }
                }
            }
        }

        //Default Select Target
        for (int i = 1; i < m_teamList.Length; i++)
        {
            if (true == character.IsAlly(TeamManager.ToFlag(i)))
            {
                continue;
            }

            //Hostage는 Priority Select에서만 적용하도록 하기 위해 예외처리
            if(TeamFlag.Hostage == TeamManager.ToFlag(i))
            {
                continue;
            }

            List<Character> enemyList = m_teamList[i];
            for (int k = 0; k < enemyList.Count; k++)
            {
                Character enemy = enemyList[k];

                Vector3 dir = enemy.transform.position - character.transform.position;
                if (Vector3.Angle(dir, character.transform.forward) > angle * 0.5f)
                {
                    continue;
                }

                if (dir.magnitude > distance)
                {
                    continue;
                }
                
                //특수 기능으로 적이 있어도 감지를 못하게 만든다.
                if(!attack)
                {
                    continue;
                }

                return enemy;
            }
        }

        return null;
    }

    public Character GetEnemyByRange(TeamFlag team, Vector3 position, Vector3 forward, float distance, float angle)
    {
        for(int i = 1; i < m_teamList.Length; i++)
        {
            if(true == team.IsAlly(TeamManager.ToFlag(i)))
            {
                continue;
            }

            List<Character> enemyList = m_teamList[i];
            for(int k = 0; k < enemyList.Count; k++)
            {
                Character enemy = enemyList[k];

                Vector3 dir = enemy.transform.position - position;

                if(Vector3.Angle(dir, forward) > angle * 0.5f)
                {
                    continue;
                }

                if(dir.magnitude > distance)
                {
                    continue;
                }

                return enemy;
            }
        }

        return null;
    }

    public List<Character> GetEnemiesByRange(Character character, float distance)
    {
        List<Character> targets = new List<Character>();

        for(int i = 1; i < m_teamList.Length; i++)
        {
            if(true == character.Team.IsAlly(TeamManager.ToFlag(i)))
            {
                continue;
            }

            List<Character> enemyList = m_teamList[i];
            for(int k = 0; k < enemyList.Count; k++)
            {
                Character enemy = enemyList[k];

                float dist = Vector3.Distance(enemy.transform.position, character.transform.position);
                if(dist > distance)
                {
                    continue;
                }

                targets.Add(enemy);
            }
        }

        return targets;
    }

    public List<Character> GetEnemiesByRange(TeamFlag team, Vector3 position, float distance)
    {
        List<Character> targets = new List<Character>();
        
        for(int i = 1; i < m_teamList.Length; i++)
        {
            if(true == team.IsAlly(TeamManager.ToFlag(i)))
            {
                continue;
            }

            List<Character> enemyList = m_teamList[i];
            for(int k = 0; k < enemyList.Count; k++)
            {
                Character enemy = enemyList[k];

                float dist = Vector3.Distance(enemy.transform.position, position);
                if(dist > distance)
                {
                    continue;
                }

                targets.Add(enemy);
            }
        }

        return targets;
    }
    
    public Character[] GetTeamCharacters(TeamFlag team)
    {
        int index = team.ToIndex();

        Character[] characters = new Character[m_teamList[index].Count];
        for(int i = 0; i < m_teamList[index].Count; i++)
        {
            characters[i] = m_teamList[index][i];
        }

        return characters;
    }

    public List<Character> GetAllCharacter()
    {
        List<Character> targets = new List<Character>();

        for (int i = 1; i < m_teamList.Length; i++)
        {
            List<Character> enemyList = m_teamList[i];
            for (int k = 0; k < enemyList.Count; k++)
            {
                Character enemy = enemyList[k];

                targets.Add(enemy);
            }
        }

        return targets;
    }

    private void ProcessAfterDeath(Character character)
    {
        if(true == character.IsAlive())
        {
            return;
        }

        switch(character.Team)
        {
            case TeamFlag.Enemy:
                {
                    break;
                }
            case TeamFlag.Boss:
                {
                    InGameManager.Instance.InGameData.TotalScore += 1000;
                    InGameManager.Instance.InGameData.WaveScore += 1000;
                }
                break;
        }
    }
}