# 던전 턴제 게임 - 구현 메모

2026 전국기능경기대회 기획서 기준으로 구현한 스크립트 구성과 씬 배선 정리.

---

## 1. 스크립트 구성

```
Assets/Script/
├─ Data/                     기획서 수치 테이블 (전부 배열)
│   ├─ GameEnums.cs          공통 열거형
│   ├─ SkillData.cs          플레이어 스킬 정의
│   ├─ EquipmentData.cs      장비 정의
│   ├─ ConsumableData.cs     소모품 정의 + InventorySlot
│   ├─ MonsterData.cs        몬스터 / 몬스터 스킬 정의
│   └─ GameData.cs           ★ 모든 수치가 여기 배열에 들어있다
├─ Battle/
│   ├─ StatusEffect.cs       버프 / 디버프 / 상태이상
│   ├─ Combat.cs             데미지 · 크리티컬 · 회피 계산
│   ├─ Monster.cs            몬스터 / 정예 / 보스 AI
│   ├─ BattleEffects.cs      전투 연출 총괄 (정적 진입점)
│   ├─ FloatingText.cs       피해 / 회복 숫자
│   ├─ HitFlash.cs           피격 시 번쩍임
│   └─ CameraShake.cs        카메라 흔들림
├─ ui/
│   ├─ BattleLog.cs          전투 로그 4줄
│   ├─ BattleUI.cs           전투 화면 버튼 연결
│   ├─ StatusUI.cs           LV / HP / MP / EXP 실시간 표시
│   ├─ EnemyUI.cs            몬스터 머리 위 HP·예고 표시
│   ├─ ShopUI.cs             상점 & 휴식
│   ├─ InventoryUI.cs        배낭 6칸 + 장비 교체
│   ├─ RankingUI.cs          TOP5 표시
│   ├─ NicknameInputUI.cs    버튼 클릭 이니셜 입력
│   ├─ ResultUI.cs           게임 오버 / 결과
│   ├─ OptionUI.cs           BGM / SFX 0~100%
│   ├─ UIButtonBinder.cs     화면 전환 버튼 코드 배선
│   └─ SliderController.cs   (기존) 슬라이더 +/- 및 값 표시
├─ Entry.cs                  전투 유닛 공통 베이스
├─ Player.cs                 플레이어 (성장 / 장비 / 배낭 / 스킬)
├─ BattleManager.cs          턴 진행
├─ StageManager.cs           3스테이지 · 방 이동 지도
├─ UIManager.cs              화면 전환 · ESC 일시정지
├─ GameManager.cs            게임 상태 · 클리어 시간 · 치트키
├─ AudioManager.cs           BGM / SFX
└─ RankingManager.cs         PlayerPrefs 랭킹 저장

Assets/Editor/GameUIBuilder.cs       누락 패널 재생성 도구
                                     (메뉴 : Tools/TurnBasedGame/Build Missing Panels)
Assets/Editor/GameEffectsBuilder.cs  전투 이펙트 생성 · 연결 도구
                                     (메뉴 : Tools/TurnBasedGame/Build Battle Effects)
```

## 1-1. 전투 연출

| 상황 | 연출 |
|---|---|
| 피격 | 주황 파티클 + 피해 숫자 + 대상 번쩍임 + 카메라 흔들림 |
| 크리티컬 | 붉은 큰 파티클 + `숫자!` 확대 표시 + 강한 흔들림 |
| 회피 | `MISS` 표시 |
| 회복 | 초록 파티클 + `+숫자` |
| 몬스터 사망 | 사망 파티클, 0.7초 뒤 사라짐 (`BattleManager.monsterFadeDelay`) |
| 플레이어 피격 | 화면 가장자리 붉은 플래시 |
| 막타 | 1.8초 대기 후 결과로 전환 (`BattleManager.battleEndDelay`) |

파티클 프리팹은 `Assets/Prefabs/Effects/` 에 있다. 에셋스토어 VFX 를 받으면
`BattleEffects` 인스펙터의 슬롯만 교체하면 된다.

어디서든 아래처럼 부를 수 있다 (인스턴스가 없으면 아무 일도 안 한다).

```csharp
BattleEffects.Hit(target, damage, isCritical);
BattleEffects.Miss(target);
BattleEffects.Heal(target, amount);
BattleEffects.Death(target);
BattleEffects.Shake(0.2f, 0.3f);
```

## 2. 밸런스 수치를 바꾸고 싶을 때

전부 `GameData.cs` 한 파일에 배열로 들어있다.

| 내용 | 배열 |
|---|---|
| 스킬 13종 | `GameData.Skills` |
| 장비 11종 (무기 6 / 갑옷 5) | `GameData.Equipments` |
| 소모품 5종 | `GameData.Consumables` |
| 몬스터 10종 | `GameData.Monsters` |
| 몬스터·보스 스킬 7종 | `GameData.MonsterSkills` |
| 레벨업 요구 경험치 | `GameData.RequiredExp` |
| 플레이어 기본값 | `GameData.BaseXxx` 상수 |

배열 인덱스가 곧 id다. 전투 화면 스킬 목록 버튼 13개도 이 인덱스 순서와 1:1로 대응한다.

## 3. 씬 배선 (Demo_Scene)

| 오브젝트 | 컴포넌트 | 연결된 것 |
|---|---|---|
| GameManager | GameManager | player |
| BattleManager | BattleManager | player, targetingCamera, monsterSlots(4), defaultMonsterPrefab, turnText, battleUI |
| UIManager | UIManager, UIButtonBinder, AudioManager | 12개 패널 + 메뉴 버튼 전체 |
| BattleArena/Player | Player | Body, TurnMark |
| BattleArena/MonsterSlots | - | Slot 0~3 (몬스터가 서는 자리) |
| Canvas/MAP | StageManager | 3스테이지 × 방 7개 = 버튼 21개 |
| Canvas/Battle | BattleUI | 공격/스킬/아이템 버튼, 스킬 목록 13개 |
| Canvas/Battle/Status, Canvas/MAP/Status | StatusUI | LV/HP/MP/EXP |
| Canvas/Ranking | RankingUI | 5줄 × (순위/이름/시간) |
| Canvas/Option | OptionUI | BGM·SFX 슬라이더 |

> 기존 버튼 인스펙터에 걸려 있던 `패널.SetActive` 배선은 `UIButtonBinder`가
> 런타임에 꺼버리고 UIManager 동작으로 바꾼다. 씬 데이터는 건드리지 않는다.

### 지도 방 라벨 (진행 상태)

| 라벨 | 뜻 | RoomStatus |
|---|---|---|
| `-` | 경로가 이어지지 않아 갈 수 없음 | Lock |
| `?` | 아직 못 간 방 | Ready |
| `O` | 지금 플레이 중 | Playing |
| `X` | 클리어 완료 | Clear |

`StageManager` 인스펙터의 `lockLabel` / `readyLabel` / `playingLabel` / `clearLabel` 에서 바꿀 수 있다.

### 방 종류 규칙

`StageManager.autoAssignRoomTypes` 가 켜져 있으면 매번 자동으로 정리된다.

- **보스 방** : 인스펙터에서 `roomType = Boss` 로 지정한 방 (스테이지당 1개, 지도의 끝)
- **상점 / 휴식** : `nextRoomIndex` 에 보스 방이 들어있는 방 = 보스 바로 앞방
- **전투** : 나머지 전부 (잡몹)

지도 배치를 바꾸면 방 종류는 알아서 따라간다. 직접 지정하고 싶으면
`autoAssignRoomTypes` 를 끄고 각 방의 `roomType` 을 손으로 설정하면 된다.

## 4. 남은 작업 (리소스 교체)

1. **몬스터 3D 모델** — 지금은 캡슐 플레이스홀더(`Assets/Prefabs/Monster_Placeholder.prefab`).
   실제 모델로 바꾸려면 프리팹의 `Body` 메시만 교체하면 된다.
   몬스터마다 다른 모델을 쓰려면 `BattleManager.monsterPrefabs` 배열(길이 10,
   `GameData.Monsters` 와 같은 인덱스)에 프리팹을 넣으면 된다. 비워두면 기본 프리팹을 쓴다.
   새 프리팹에는 `Monster` + `Collider`(대상 클릭용) + 자식에 `EnemyUI` 가 있어야 한다.
2. **플레이어 3D 모델** — `BattleArena/Player/Body` 교체.
3. **BGM / SFX** — `AudioManager` 인스펙터의 AudioClip 슬롯에 넣으면 된다.
   (현재 프로젝트에 있는 오디오는 버튼 클릭/호버 2개뿐)
4. **스테이지 2·3 지도 디자인** — `Canvas/MAP/Stage2`, `Stage3` 는 Stage1 을 복제한 것이라
   방 배치가 동일하다. 방 위치·경로 이미지를 옮기고, 필요하면 `StageManager.stages[n].rooms[].nextRoomIndex`
   와 `roomType` 을 인스펙터에서 조정하면 된다.

## 5. 기획서 해석이 갈릴 수 있는 부분 (구현 선택)

- **회피의 물약** : 지속 턴이 기획서에 없어 *전투 종료까지* 유지되도록 했다.
  (`GameData.Consumables[4].effectTurns = StatusEffect.UntilBattleEnd`)
- **닉네임 "2개 단어 이상"** : *2글자 이상* 으로 해석했다.
  `NicknameInputUI.minLength` 로 바꿀 수 있다.
- **방어력 10% 하한** : 방어력에 의한 감소에만 적용하고, 몬스터 '방어'의
  (50 + 레벨×3)% 감소는 그 뒤에 따로 곱한다.
- **정예 몬스터 스킬** : 기획서에 스킬 내용이 없어 `강타`(150%, 쿨 3턴)를 새로 정의했다.
- **상점 장비/스킬북 가격** : "밸런스에 맞게" 라고만 되어 있어 임의로 정했다. `GameData` 에서 수정 가능.
- **몬스터 스킬 시작 쿨타임** : 전투 시작 직후 최상위 스킬이 나오지 않도록
  쿨타임의 절반을 채운 상태로 시작한다.

## 6. 치트키

| 키 | 기능 | 키 | 기능 |
|---|---|---|---|
| F1 | 무적 토글 | F6 | 현재 전투 적 전멸 |
| F2 | 공격력 +100 | F7 | 메인화면 |
| F3 | HP 최대 회복 | F8 | 1스테이지 이동 |
| F4 | MP 최대 회복 | F9 | 2스테이지 이동 |
| F5 | 레벨 +1 | F10 | 3스테이지 이동 |

## 7. 조작

- 전투 : `공격` / `스킬` / `아이템` 버튼 클릭 → 대상은 몬스터를 마우스로 클릭하거나 숫자키 1~4
- 대상 선택 취소 : ESC
- 일시정지 : ESC (게임 진행 중) — 다시 ESC 로 해제
- 배낭 : 화면 좌상단 Status 안의 버튼
