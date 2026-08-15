using UnityEngine;

public class GameManager : MonoBehaviour
{
    static public GameManager instance;

    public Player player;

    public bool isDebugF1;

    private void Awake()
    {
        instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugF1();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugF2();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugF3();
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            DebugF4();
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            DebugF5();
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            DebugF6();
        }
        else if (Input.GetKeyDown(KeyCode.F7))
        {
            DebugF7();
        }
        else if (Input.GetKeyDown(KeyCode.F8))
        {
            DebugF8();
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            DebugF9();
        }
        else if (Input.GetKeyDown(KeyCode.F10))
        {
            DebugF10();
        }
    }

    void DebugF1()
    {
        // 플레이어 무적
        isDebugF1 = !isDebugF1;
    }

    void DebugF2()
    {
        // 캐릭터 공격력 증가 100
        player.atk += 100;
    }

    void DebugF3()
    {
        // hp 최대 회복
        player.hp = player.maxhp;
    }

    void DebugF4()
    {
        // mp 최대 회복
        player.mp = player.maxmp;
    }

    void DebugF5()
    {
        // 플레이어 레벨 1업
        player.level += 1;
    }

    void DebugF6()
    {
        // 현재 전투의 모든 적 제거
        
    }
    void DebugF7()
    {
        // 메인 화면
    }
    void DebugF8()
    {
        // 1스테이지 이동
    }
    void DebugF9()
    {
        // 2스테이지 이동
    }
    void DebugF10()
    {
        // 3스테이지 이동
    }
}
