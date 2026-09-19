using UnityEngine;
using UnityEngine.UI;

// ═════════════════════════════════════════════════════════════════════════════
//  StatusUI.cs  —  플레이어 정보를 화면에 계속 보여준다
//
//  기획서 6) UI 필수 요소 :
//    레벨, 경험치, MP, HP, 스킬, 보관함  ← 이 중 레벨/경험치/MP/HP/골드/장비 담당
//
//  ★ 기획서 2-2-2 : 경험치는 [현재 경험치] / [레벨업 필요 경험치] 로 표기해야 한다.
//
//  ★ 이 스크립트는 전투 화면과 지도 화면 양쪽에 똑같이 붙여서 쓴다.
//    LateUpdate 에서 매 프레임 새로 그리므로 따로 갱신을 부를 필요가 없다.
// ═════════════════════════════════════════════════════════════════════════════

public class StatusUI : MonoBehaviour
{
    [Header("레벨")]
    public Text levelText;

    [Header("HP")]
    public Slider hpSlider;
    public Text hpText;

    [Header("MP")]
    public Slider mpSlider;
    public Text mpText;

    [Header("경험치")]
    public Slider expSlider;
    public Text expText;

    [Header("골드")]
    public Text goldText;

    [Header("착용 장비")]
    public Text weaponText;
    public Text armorText;

    [Header("능력치 요약")]
    public Text statText;

    // LateUpdate 는 Update 가 전부 끝난 뒤에 실행된다.
    // 전투 계산이 다 끝난 뒤의 값을 보여주기 위해 여기서 그린다.
    private void LateUpdate()
    {
        Player 플레이어 = GetPlayer();
        if (플레이어 == null) return;

        // ── 레벨
        if (levelText != null) levelText.text = "Lv. " + 플레이어.level;

        // ── HP
        SetBar(hpSlider, hpText, 플레이어.hp, 플레이어.maxHp);

        // ── MP
        SetBar(mpSlider, mpText, 플레이어.mp, 플레이어.maxMp);

        // ── 경험치 (기획서 : 현재 / 필요 로 표기)
        if (플레이어.IsMaxLevel())
        {
            // 최고 레벨이면 꽉 찬 바에 MAX 라고 보여준다.
            if (expSlider != null)
            {
                expSlider.minValue = 0f;
                expSlider.maxValue = 1f;
                expSlider.value = 1f;
            }
            if (expText != null) expText.text = "MAX";
        }
        else
        {
            SetBar(expSlider, expText, 플레이어.exp, 플레이어.GetNeedExp());
        }

        // ── 골드
        if (goldText != null) goldText.text = 플레이어.gold + " G";

        // ── 착용 장비
        if (weaponText != null)
        {
            EquipData 무기 = GameData.GetEquip(플레이어.weaponNum);
            if (무기 != null) weaponText.text = "무기 : " + 무기.name;
            else weaponText.text = "무기 : -";
        }

        if (armorText != null)
        {
            EquipData 갑옷 = GameData.GetEquip(플레이어.armorNum);
            if (갑옷 != null) armorText.text = "갑옷 : " + 갑옷.name;
            else armorText.text = "갑옷 : -";
        }

        // ── 능력치 요약
        if (statText != null)
        {
            statText.text = "공격 " + 플레이어.GetFinalAtk()
                          + "   방어 " + 플레이어.GetFinalDef()
                          + "   속도 " + 플레이어.speed
                          + "\n크리 " + 플레이어.GetFinalCrit() + "%"
                          + "   회피 " + 플레이어.GetFinalEvade() + "%";
        }
    }

    /// <summary>바(Slider)와 글자를 "현재 / 최대" 모양으로 채운다.</summary>
    private void SetBar(Slider slider, Text text, int now, int max)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // 0으로 나누면 에러가 나므로 먼저 확인한다.
            if (max <= 0) slider.value = 0f;
            else slider.value = Mathf.Clamp01((float)now / max);
        }

        if (text != null) text.text = now + " / " + max;
    }

    /// <summary>플레이어를 가져온다. 없으면 null.</summary>
    private Player GetPlayer()
    {
        if (GameManager.instance == null) return null;
        return GameManager.instance.player;
    }
}
