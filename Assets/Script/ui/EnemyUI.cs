using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 프리팹에 붙는 월드 스페이스 UI.
/// 이름 / HP / 이번 턴 행동 예고(방어, 파괴 광선)를 보여준다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class EnemyUI : MonoBehaviour
{
    [Header("대상")]
    public Monster monster;

    [Header("표시")]
    public Text nameText;
    public Slider hpSlider;
    public Image hpFill;
    public Text hpText;

    [Header("예고 / 상태")]
    [Tooltip("이번 턴 '방어'를 사용한다는 표시")]
    public GameObject defendIcon;
    [Tooltip("파괴 광선처럼 예고가 필요한 스킬 표시")]
    public GameObject telegraphIcon;
    public Text intentText;

    [Header("선택 하이라이트")]
    public GameObject highlight;

    [Header("카메라 바라보기")]
    public bool faceCamera = true;

    private Camera mainCam;

    private void Awake()
    {
        if (monster == null) monster = GetComponentInParent<Monster>();
        if (highlight != null) highlight.SetActive(false);
    }

    private void LateUpdate()
    {
        if (monster == null) return;

        UpdateHp();

        if (faceCamera)
        {
            Camera cam = ResolveCamera();
            if (cam != null)
            {
                // 캔버스의 앞면(+Z)이 카메라가 보는 방향과 같아야 글자가 바로 보인다.
                transform.rotation = cam.transform.rotation;
            }
        }
    }

    private void UpdateHp()
    {
        if (nameText != null)
        {
            nameText.text = string.Format("{0} Lv.{1}", monster.entryName, monster.level);
        }

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = monster.HpRatio;
        }

        if (hpFill != null)
        {
            hpFill.fillAmount = monster.HpRatio;
        }

        if (hpText != null)
        {
            hpText.text = string.Format("{0} / {1}", monster.hp, monster.maxHp);
        }
    }

    /// <summary>이번 턴 행동 예고를 갱신한다. BattleManager 가 라운드마다 호출한다.</summary>
    public void Refresh()
    {
        if (monster == null) return;

        bool willDefend = monster.IsAlive && monster.WillDefend;
        MonsterSkillData telegraphed = monster.IsAlive ? monster.TelegraphedSkill : null;

        if (defendIcon != null) defendIcon.SetActive(willDefend);
        if (telegraphIcon != null) telegraphIcon.SetActive(telegraphed != null);

        if (intentText != null)
        {
            if (telegraphed != null) intentText.text = string.Format("!! {0} !!", telegraphed.skillName);
            else if (willDefend) intentText.text = "방어";
            else intentText.text = string.Empty;
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetActive(on);
    }

    /// <summary>
    /// 씬 카메라에 MainCamera 태그가 없을 수도 있으므로 BattleManager 쪽 카메라를 먼저 본다.
    /// </summary>
    private Camera ResolveCamera()
    {
        if (mainCam != null) return mainCam;

        if (BattleManager.instance != null && BattleManager.instance.targetingCamera != null)
            mainCam = BattleManager.instance.targetingCamera;
        else if (Camera.main != null)
            mainCam = Camera.main;
        else
            mainCam = FindFirstObjectByType<Camera>();

        return mainCam;
    }
}
