---
name: sommoje-plan
description: Sommoje(소모제) 캐주얼 SRPG 개발 로드맵과 진행 상황. 다음에 뭘 만들지, 어디까지 했는지 확인할 때 사용. Use when resuming work on the sommoje game to check the build plan and progress.
---

# Sommoje — 캐주얼 그리드 SRPG 개발 플랜

## 게임 컨셉
- **장르**: 캐주얼 TRPG / 그리드 기반 SRPG (파이어엠블렘 · 인투 더 브리치 스타일)
- **핵심 메커닉**: 턴마다 격자 위를 이동, 근접 공격 / 활·마법 원거리 공격
- **특징 시스템**: **주사위를 굴려 기력(에너지)을 얻고**, 기력으로 행동/스킬 사용
- **월드/기후**: 지구 느낌의 **위도 기반 기후대 맵** — 북극·남극은 춥고(빙하/눈), 적도는 덥다(사막/정글). 맵 Y좌표 → 기후값 → 타일종류로 연결. 기후가 게임플레이(기력 회복·이동력 등)에 영향 줄지 검토 중(A 비주얼만 / B 메커닉 영향).
- **구조**: 고전 SRPG식 **Scene 분리형** (오픈월드 ❌). Title → WorldMap → Battle → (Town) Scene으로 화면 전환.
  - **WorldMap Scene**: 위도 기반 기후대 맵, 파티가 지점 사이 이동, 전투 지점 진입
  - **Battle Scene**: ⭐ 실제 그리드 전투(타일맵+이동+공격+턴+기력). 전투 지형은 월드맵 기후와 연동 가능
  - Scene 간 데이터(파티 상태, 전투지점, 결과)는 `DontDestroyOnLoad` 매니저로 전달
- **엔진**: Unity + C# (2D)
- **플랫폼**: **모바일, iOS 먼저** / **가로(Landscape) 고정**. 터치 입력은 레거시 `Input.GetMouseButtonDown(0)`이 첫 터치를 마우스로 매핑해줘서 그대로 동작. 카메라는 `CameraFitter`로 화면비 자동 맞춤. iOS 실기기 빌드는 나중에(Hub에 iOS Build Support + Xcode/Apple 계정 필요).

## 개발 순서 (핵심 전투 루프 우선)
> 토대(타일맵) → 움직임 → 턴 → 전투 → 기력 → 아이템 → AI
> 아이템보다 전투를 먼저 — "전투가 재밌는가"를 가장 먼저 검증한다.

- [x] **1. 타일맵 + 격자 좌표계** — 코드 생성 그리드 완성, 위도 기후색 적용, [ExecuteAlways]로 편집모드 표시, 헤드리스 렌더 캡처로 검증 완료(`battle_preview.png`)
- [x] **2. 캐릭터 배치 + 이동** — Unit(원형 스프라이트), 클릭 선택→BFS 이동범위(파란칸)→클릭 이동. 헤드리스 캡처로 검증. (Play에서 인터랙션)
- [x] **3. 턴 시스템** — Team(Player/Enemy), 유닛별 HasActed(행동완료 시 어둡게), 플레이어 턴 이동→자동/수동 End Turn→적 턴(임시 랜덤이동, 정식 AI는 7단계)→라운드 증가. IMGUI로 "Round N/턴" + End Turn 버튼. 2:2 배치
- [x] **4. 전투 (근접/원거리)** — HP/공격력/사거리, 체력바, 근접(돌진)·원거리(투사체) 공격, 이동 후 공격, 적 AI(가까운 적으로 이동→사거리면 공격), 승리/패배 판정, 대기 버튼. ⭐게임의 심장 (counterattack/방어력은 추후)
- [x] **5. 기력(주사위) 시스템** — 매 플레이어 턴 d6 굴려 기력 획득(파티공용 풀, 최대10). 이동=무료/기본공격=1기력/스킬=3기력. 전사"강타"·궁수"꿰뚫기"(+4뎀). 상단 기력게이지+주사위눈 표시
- [ ] **6. 아이템 시스템** — 무기/장비/소비 아이템 (전투 완성 후 밸런스)
- [ ] **7. 적 AI** — 처음엔 "가장 가까운 적에게 이동 후 공격" 수준

## 마일스톤
- **첫 프로토타입 목표**: 1~4단계 = 캐릭터 움직여서 적 때리는 플레이 가능 버전

## ⚠️ 방향 전환 검토 중 (2026-06-03)
- 사용자가 **3D 액션 RPG**에 관심 (3D 몰입감 + 깔끔한 UI(체력바 없음) + **QWER 스킬**). 로스트아크/원신 스타일.
- 결정: **3D 액션 "맛보기 데모"를 먼저** 만들어 느낌 체험 후 본격 전환 여부 결정. 2D SRPG 자산은 거의 재사용 불가(장르가 다름)임을 사용자도 인지.
- 데모 방식: 도형(캡슐/큐브) 플레이스홀더 → 좋으면 Mixamo/Synty 무료 에셋 교체. 기력→QWER 마나로 재활용.
- **백로그 아이디어**: 소셜/관계망 시스템(친해지면 누구와 멀어짐, 관계상태로 퀘스트 해금 — 페르소나 소셜링크식). 데이터/로직이라 2D·3D 무관하게 추가 가능. 탐험형 월드맵 + 안개도 후보.
- **3D 데모 만듦**(같은 프로젝트, Action3D 씬, 도형 플레이스홀더): `Assets/Scripts/Action3D/`(PlayerController3D 3인칭이동, ThirdPersonCamera 우클릭회전, SkillSystem QWER+마나+쿨다운, Enemy3D 추격더미), `Assets/Editor/Action3DSceneBuilder.cs`(Build/Capture), `Assets/Scenes/Action3D.unity`. 조작: **좌클릭 이동(로스트아크식, 클릭마커 표시)** / 우클릭드래그 카메라 / QWER 스킬. (WASD↔W스킬 충돌 때문에 클릭이동으로 변경). 검증: `action3d_preview.png`. 레거시 Input 사용(새 인풋시스템 없음).
- **엔진 논의**: 3D라도 이 케이스(첫 게임·솔로·모바일iOS·캐주얼·내 도움)엔 Unity 권장. 언리얼은 고사양/포토리얼·PC콘솔에 유리하나 모바일·초보·내 지원에서 불리.
- **✅ 3D로 방향 확정(2026-06-03)**. 같은 프로젝트 유지(빌트인 파이프라인), 에디터 3D모드로 전환(`m_DefaultBehaviorMode=0`). 조작은 클릭이동+QWER. 정리 필요시 추후 새 프로젝트.
- **지형 추가**: `Action3DSceneBuilder.GenerateTerrain` — 펄린노이즈 Terrain(중앙 평지/가장자리 언덕), 코드생성 풀 TerrainLayer(`Assets/Art/Terrain/`). Enemy3D는 `Terrain.activeTerrain.SampleHeight`로 지형에 붙음.
- **풍경 에셋 적용**: Kenney **Nature Kit**(CC0) FBX를 `Assets/Art/Nature/`에 복사(나무/바위/꽃/풀). FBX에 머티리얼 색 박혀있어 Unity가 자동 임포트. `Action3DSceneBuilder.PlaceModel`로 지형 위 배치, `PropScale=2.5`(native ~1.2~1.7유닛 보정). 다운로드 URL: kenney.nl/media/.../kenney_nature-kit.zip
- **관절 캐릭터**: 도형 조립 휴머노이드(`BuildCharacter`)+`CharacterAnimator`(걷기/공격, 위치델타로 속도감지). 카메라는 쿼터뷰 시도→**3인칭으로 롤백**(오픈월드 몰입감 선호).
- **리얼 데모 실험(진행중)**: 사용자가 "데모까지 리얼하게 만들어보고 판단" 결정.
  - 지형: **호수(중앙)+산(가장자리) 계곡**으로 재생성(높이 60, dist<0.22 호수분지/<0.55 평지/그외 산). 물 `BuildWater`(Standard 투명, WaterY=7.2).
  - 리얼 잔디: AmbientCG **Grass004** CC0 (`Assets/Art/Terrain/Real/` color+normal), TerrainLayer tileSize 6.
  - 하늘+조명: PolyHaven **HDRI**(`Assets/Art/Sky/sky.hdr`), `SetupSky`로 Skybox/Panoramic + ambientMode=Skybox(IBL).
  - 다운로드법: AmbientCG `ambientcg.com/get?file=<ID>_1K-JPG.zip`, PolyHaven HDRI는 api.polyhaven.com/files/<id>.
- **⚠️ 핵심 교훈**: 아트는 스타일 통일이 중요. 리얼 땅+만화 나무/캐릭터는 어색. 리얼로 갈거면 전부 리얼(무겁고 모바일과 멀어짐). 사용자에게 "끝내는 게 먼저, 아트는 마지막" 조언함.
- **리얼 플레이어 요청**: 자동 다운로드 불가(포토리얼 무료 캐릭터 희박). 경로 = **Mixamo**(무료 Adobe 로그인, 수동 다운로드) → 내가 Animator 연결. 또는 ReadyPlayerMe(GLB, glTFast 필요).
- **멀티텍스처 지형**: 잔디(Grass004)/바위(Rock023)/흙(Ground003) 3 TerrainLayer + 높이·경사 스플랫맵(`PaintSplatmap`). 레이어 smoothness=0(무광).
- **색공간 Linear 전환**(`m_ActiveColorSpace: 1`) — 과노출/허옇게 뜨는 거 완화(에셋 전체 재임포트됨).
- **후처리(Post-processing v2)**: `com.unity.postprocessing` 3.4.0 설치(충돌 없음). `SetupPostProcessing`로 PostProcessLayer(FXAA)+global Volume+profile(ACES 톤매핑/채도+18/대비+12, AO 0.75, Bloom, Vignette). ⚠️ **후처리는 헤드리스 `cam.Render()` 캡처에 안 잡힘 → Play에서만 보임**.
- **물 셰이더**: `BuildWater` Standard 투명+매끈(반사)+코드생성 물결 노멀맵(`GenerateWaterNormal`, 사인파 합성)+`WaterAnimator`(노멀 오프셋 스크롤). 반사는 캡처에 보임, 물결 애니는 Play.
- **에디터 실행 안정화**: `open -a Unity`가 가끔 바로 꺼짐 → **에디터 바이너리 직접 실행**(`.../Unity -projectPath ... &`)이 안정적.
- **Play모드 캡처**: 후처리/풀/물결/애니는 헤드리스 `cam.Render()`(편집모드)에 안 잡힘 → `CapturePlay`(EnterPlaymode+DisableDomainReload, 50프레임 후 cam.Render→PNG, EditorApplication.Exit). `-quit` 없이 실행. 결과 `action3d_play.png`.
- **✅ Mixamo 캐릭터 적용**: Brute(`Assets/Art/Character/` Brute.fbx + Idle/Walking/Attack.fbx). `MixamoSetup.Setup`(메뉴): 캐릭터 Humanoid+CreateFromThisModel→BruteAvatar, 애니 Humanoid+CopyFromOther+loop, **ExtractTextures**(하얀석고상 방지 필수), AnimatorController(Idle⟷Walk[Moving], AnyState→Attack[trigger]). 빌더 `AttachMixamoCharacter`로 도형 대신 모델 부착(키 1.9 자동스케일, 발 -1, applyRootMotion off). 런타임 `MixamoCharacter`(이동→Moving bool, `Attack()`=trigger). SkillSystem이 스킬 시 Attack 호출.
- **✅ 리얼 나무**: Sketchfab API(토큰)로 "Maple trees pack"(CC-BY) source FBX. `Assets/Art/Trees/AcerTreePack.fbx`(나무 12종+Ground). `TreeSetup.AssignMaterials`로 Bark_Mat(bark_basecolor+normal)/Cluster_Mat(잎 albedo+opacity 합친 Cluster_leaf_RGBA, 알파컷아웃 _Mode1 _Cutoff0.4) 수동 연결(외부텍스처라 자동 안 됨). 빌더 `AttachTrees`: 개별 나무 클론, **Billboard 머티리얼 렌더러 제거**(흰카드 방지), 키정규화 호수 둘러 배치. **CC-BY 출처표기**(`CREDITS.md`).
- Sketchfab 다운로드 팁: source/glb url 추출 시 **끝 따옴표 제거**(`s/"$//`), URL 금방 만료→즉시 다운. /tmp 쓰기 샌드박스가 막을 때 있음→프로젝트폴더로. python3 멀티라인 가끔 막힘.
- **🎉 리얼 데모 풀세트 완성**(2026-06-03): 지형(멀티텍스처)+Linear+후처리+HDRI하늘+호수(반사물결)+산+리얼나무+Mixamo캐릭터(걷기/공격).
- **캐릭터 교체 Erika 궁수**: `Erika.fbx`. `MixamoSetup.SetupErika`(Humanoid+ExtractTextures). 애니는 Humanoid라 자동 리타겟. `AttachMixamoCharacter`가 Erika.fbx 로드.
- **로코모션(블렌드트리)**: Erika 애니 4종(Idle/Walk/Run/Attack `Erika_*.fbx`). `MixamoSetup.SetupErikaLocomotion`: Humanoid+CopyFromOther(Erika아바타)+loop, 1D BlendTree(Speed: idle0/walk2/run5)+Attack(trigger). `MixamoCharacter`가 위치델타 속도→`SetFloat("Speed")`. `PlayerController3D`: walkSpeed2/runSpeed5/runDistance10(멀면 달리기)/decelDist2.5(도착감속). 사용자: 걷기 2 선호.

## 환경 메모
- 작업 폴더: `/Users/jaehoon/workspace/sommoje`
- git: 초기화 완료, user `rival0605` / `rival0605@gmail.com` (로컬 설정)
- `.gitignore`: Unity 표준 적용 완료
- **Unity Editor: `6000.4.9f1` (Unity 6 LTS)**, 경로 `/Applications/Unity/Hub/Editor/6000.4.9f1/`
- **렌더 파이프라인: Built-in** (URP 안 씀 — 캐주얼 2D라 단순하게)
- 2D 모드 ON(`EditorSettings.m_DefaultBehaviorMode=1`), 패키지: `2d.sprite`, `2d.tilemap.extras`
- ⚠️ **Input System 패키지는 이 에디터와 충돌(BuildTarget.ReservedCFE)** → 제거함. **레거시 Input(`Input.GetMouseButton` 등) 사용**
- 코드 네임스페이스: **`Sommoje.Battle`** (`Sommoje.Grid`는 UnityEngine.Grid와 충돌하니 금지)
- 배치 빌드: `Unity -projectPath ... -batchmode -quit -nographics -executeMethod BattleSceneBuilder.Build -logFile unity_build.log`
- **화면 검증법**: GUI 에디터가 열려있으면 프로젝트가 잠겨 배치 실행 불가 → 먼저 `osascript -e 'tell application "Unity" to quit'`로 닫고, `BattleSceneBuilder.Capture` (배치, **-nographics 빼고**) 실행 → `battle_preview.png` 생성됨. 이걸 Read로 직접 확인.
- ⚠️ 스크립트 수정해도 **열려있는 에디터가 자동 재컴파일 안 하면 화면 그대로** → 포커스 주거나 Cmd+R, 안 되면 에디터 재시작 필요.

## 에셋
- **Kenney Tiny Dungeon + Tiny Town (CC0, 무료)** 사용. 원본 다운로드는 `_assetdl/`(gitignore). 16x16 픽셀.
- 게임용 스프라이트: `Assets/Resources/Kenney/` → warrior(타일85)/archer(112)/slime(108)/crab(110)/floor_grass(town0). `Resources.Load<Sprite>("Kenney/<name>")`로 로드
- `Assets/Editor/KenneyImport.cs` (AssetPostprocessor): Resources/Kenney PNG를 Sprite/Point/16PPU/무압축으로 자동 임포트
- 캐릭터 타일은 어두운 토큰 외곽선이 그림에 포함됨(정상). 유닛 color=흰색(틴트X), 행동완료 시만 어둡게
- ⚠️ 더 받을 땐: Kenney 다운로드 URL은 `WebFetch`로 페이지에서 "Continue without donating" zip 링크 추출 → curl

## 주요 파일
- `Assets/Scripts/Grid/BattleGrid.cs` — 격자 생성(ExecuteAlways), 셀↔월드 변환, 기후색, `Highlight()`/`Reachable` 지원 API
- `Assets/Scripts/Battle/Unit.cs` — 캐릭터(원형 스프라이트), `Cell`/`moveRange`, `PlaceAt`
- `Assets/Scripts/Battle/CameraFitter.cs` — 화면비에 맞춰 격자 전체 보이게 직교 카메라 자동 맞춤(ExecuteAlways)
- `Assets/Scripts/Battle/BattleController.cs` — 입력/선택/이동범위(BFS)/이동. **RuntimeInitializeOnLoadMethod로 Play 시 자동 생성**(씬 수정 불필요). 색상 상수, `SpawnDemo`/`Reachable` static 제공
- `Assets/Editor/BattleSceneBuilder.cs` — 씬 구성(`Build`) + 렌더 캡처(`Capture`, 유닛/이동범위 미리보기 포함). 메뉴: **Sommoje ▸ ...**
- `Assets/Scenes/Battle.unity` — 전투 씬 (12×8, 위도 그라데이션 ON). 유닛은 Play 시 자동 스폰

## 입력/좌표 메모
- 클릭→셀: `Camera.main.ScreenToWorldPoint(Input.mousePosition)` → `BattleGrid.WorldToCell`
- 유닛 sortingOrder=5 (타일맵 ground=0, overlay=1 위)
- 데모 배치(2:2): 전사(2,3 이동4 hp14 atk5 근접) / 궁수(3,5 이동4 hp10 atk4 사거리3 원거리) / 적1(9,4)·적2(8,2 이동3 hp11 atk4 근접)
- 전투 흐름: 선택→이동범위(파랑)+사거리내 적(빨강) 표시→빈칸 이동(1회)→사거리 적 클릭=기본공격(1기력) or 스킬버튼(3기력) or 대기. 공격=돌진/투사체 연출+데미지+체력바+사망제거. 적턴 AI=최근접 플레이어로 이동 후 사거리면 공격. CheckBattleEnd로 승/패
- 기력: `_energy`(0~10), 턴시작 `RollDice` 코루틴(d6, _busy로 입력잠금). `PlayerAttack(attacker,target,cost,damage)`로 기본/스킬 통합. 스킬데미지=atk+4. 적은 기력 안 씀
- UI 버튼 3개(End Turn/대기/스킬) 스택, Update에서 각 Rect 위 클릭은 무시
- UI는 임시 **IMGUI(OnGUI)** — 정식 uGUI/UI Toolkit으로 교체 예정 (모바일 터치도 OnGUI 동작함). OnGUI는 편집모드 렌더 캡처엔 안 잡힘
- 턴 흐름: `StartTurn(team)`→행동완료 리셋 / 플레이어 전원 행동 시 `CheckPlayerTurnEnd`로 자동 종료 / `EndTurn`은 Player↔Enemy 토글, Enemy 끝나면 _round++

## 진행 상황 로그
- 환경 셋업(git, .gitignore) 완료.
- Unity 6 LTS 설치 확인 → 배치모드로 프로젝트 생성 → 2D 패키지/모드 세팅.
- **1단계 그리드 타일맵 코드로 완성** (위도 기후색 적용), 컴파일 에러 0. 에디터 GUI 띄워서 Play 확인 단계.
