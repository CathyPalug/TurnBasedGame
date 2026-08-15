using UnityEngine;

public class Player : MonoBehaviour
{
    public int hp;
    public int maxhp;

    public int mp;
    public int maxmp;

    public int def;
    public int atk;
    public int speed;

    public int inventorySpace;

    public int critChance;
    public int miss;

    public int level;
    public int exp;
    public int[] requireExp = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetExp(int _exp)
    {
        exp += _exp;
        if (exp >= requireExp[level - 1])
        {
            LevelUp();
        }
    }
    public void LevelUp()
    {
        exp -= requireExp[level - 1];
        level += 1;
    }
}
