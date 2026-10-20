# DUOVEX v0.7.5 — ESC PAUSE & LIGHT IT UP NORMAL/HARD (Unity 6.6)

## v0.7.5 업데이트 (2026-10-20)

- **ESC**: 게임 도중 일시정지 → 다시 ESC로 같은 위치에서 재개. 음악 재생과 판정용 DSP 시계가 함께 정지·재설정됩니다. 점수·콤보·남은 노트는 그대로 유지합니다.
- 일시정지 화면: **RESUME / RETRY / TRACK SELECT** 버튼 추가. 게임 도중 SPACE 일시정지 토글도 유지합니다.
- 곡 선택: **LIGHT IT UP / NORMAL**과 **LIGHT IT UP / HARD**가 각각 별도 곡 목록 항목으로 표시됩니다. 같은 MP3를 공유합니다.
- NORMAL: **454개 노트**, 동타 13회, F+J DUAL 11회, HOLD 6개. 축제 시연에 적합한 입문-중급.
- HARD: **713개 노트**, 동타 36회, F+J DUAL 14회, HOLD 6개. 실제 드럼·고음역 어택을 반영한 8분박과 16분박 연타(40개 노트)를 포함합니다.
- 두 채보 모두 **128 BPM / 시작 기준 7.412초**, 구간별 강약·브레이크를 따릅니다. 음악에 직접 맞춘 데이터지만 **Unity 실기 싱크 검수는 아직 필요합니다.**
- 이전 v0.7.4 씬은 덮어쓰지 않고 새 **`DuovexV075.unity`**를 생성합니다.
- 기술적 세부 내용: `LIGHT_IT_UP_DIFFICULTIES_v0.7.5.md`, `CHANGELOG_v0.7.5.md`.

**실행 순서:** 전체 프로젝트를 Unity Hub에서 열거나, 패치 파일을 기존 v0.7.4 프로젝트에 병합한 뒤 Unity 메뉴 `Tools > Duovex > Generate Editable UI Scene`을 실행합니다. 새 `Assets/Scenes/DuovexV075.unity`를 열고 Play 버튼을 누르세요. `Tools > Duovex > Run v0.7.5 Baseline Check`로 에셋·UI 검사를 할 수 있습니다.

**중요:** 아래 v0.7.3/v0.7.4 설명과 달리 이번 버전의 음악 플레이는 기존 `LightItUp.json` 대신 **`LightItUpNormal.json` / `LightItUpHard.json`**을 사용합니다. 이전 `LightItUp.json`은 보존했습니다.

---

이번 버전은 v0.7.3 기반으로 **메인·곡 선택 버튼 스타일, 음악 볼륨 슬라이더, 선명한 판정선과 키 간격**을 개선했습니다. 기존 `LIGHT IT UP` 음악 채보와 3개 튜토리얼/연습 채보는 유지했습니다. 판정 허용 시간이나 채보 포맷은 변경하지 않았습니다.

## v0.7.4 업데이트

- 메인과 곡 선택의 주요 버튼: 사이언/네이비 대비, 프레임·번호·화살표 장식, 텍스트 크기와 가독성 개선
- 곡 목록: 선택 행 대비 강화, 비어 있는 목록 위치의 악센트 잔상 제거
- 옵션: **MUSIC VOLUME**과 **HIT SOUND VOLUME**을 독립적으로 조절. 둘 다 PlayerPrefs에 보존
- 게임 판정선: `Game_HitLine`(288×8)과 `Game_HitCore`(288×2)의 **동일 중심 Y=586**
- 노트: 판정 시점에서 `Note()`의 중앙 Y값이 HitY와 일치하는 기존 판정·렌더 규칙 유지
- D/F/J/K 키 배경: 맨 위 Y=618, 판정선 중심에서 32px 아래로 이동
- 이전 씬을 보존하기 위해 **`DuovexV074.unity`**라는 새로운 씬을 생성
- 자세한 수치와 테스트 안내: `PLAYFIELD_SPECS_v0.7.4.md`, `CHANGELOG_v0.7.4.md`

## 새 음악 채보 (v0.7.3)

- 곡: Robin Hustin & Tobimorrow — Light It Up [NCS Release]
- 길이 약 3:05 / **128 BPM** / 첫 기준 박자 **7.412초** (`offsetMs=7412`)
- 520개 논리 노트 / 21회 DUAL / 6개 HOLD
- 첫 드롭 00:52.4, 브레이크 01:22.4, 두 번째 드롭 02:14.9
- 곡 선택에서 **MUSIC / LIGHT IT UP**을 클릭하면 바로 음악과 채보가 함께 실행됩니다.
- 게임 내 채보 에디터에서는 기본 제공 채보처럼 읽기 전용으로 취급합니다. JSON을 수정하거나 `tools/generate_light_it_up_chart.py`를 사용하세요.
- `LIGHT_IT_UP_CHART_GUIDE.md`에서 구간·설계 및 시험 방법을 확인하세요.

**권리 주의:** 이 MP3는 사용자가 제공한 사적 개발/테스트용 사본입니다. NCS의 영상 사용 허용이 게임 배포까지 허용한다는 뜻은 아닙니다. 축제 출품·배포·Steam 출시 전에 NCS의 게임 사용 관련 라이선스 문의 절차를 진행하세요: https://ncs.io/usage-policy

## 설치

**기존 프로젝트 패치**
1. Unity 에디터에서 Play를 종료하고 프로젝트 폴더를 백업합니다.
2. 패치 ZIP 안의 `Assets` 폴더를 프로젝트에 병합합니다.
3. Unity에서 컴파일 오류가 없는지 확인합니다.
4. `Tools > Duovex > Generate Editable UI Scene`을 실행하여 **새 `DuovexV075.unity` 씬**을 생성합니다. 이전 씬을 열면 이전 디자인 그대로 보입니다.
5. `DuovexV075.unity` 씬에서 Play를 누릅니다. 곡 선택 화면에 LIGHT IT UP NORMAL/HARD가 각각 표시됩니다.
6. 기존 프로젝트에 `DuovexTheme.asset`이 있었다면 Inspector에서 `noteHeight=16`, `noteSideInset=6`인지 확인합니다. 다르면 `Tools > Duovex > Apply v0.7.2 Note Metrics to Theme`으로 변경할 수 있습니다.
7. `Tools > Duovex > Run v0.7.5 Baseline Check`를 실행하고 Console 결과를 확인합니다.

**새 프로젝트로 실행**
전체 프로젝트 ZIP의 `duovex-unity` 폴더를 Unity Hub에서 Add Project from Disk로 열고, 위의 3~5번을 진행합니다.

- 기존 `Assets/Scenes/DuovexEditable.unity`, `DuovexV07.unity`, `DuovexV071.unity` 및 `Main.unity`는 덮어쓰지 않습니다.
- 생성기는 `DuovexV075.unity`를 새로 만들며 같은 이름의 씬이 있으면 덮어쓰지 않고 종료합니다.
- 생성 씬이 마음에 들면 `File > Save As...`로 자신의 씬을 복제해 작업할 수 있습니다.
- Play 전 `DuovexV075.unity`를 열어두세요. 기존 씬은 자동으로 레이아웃이 변경되지 않습니다.

## 무엇을 편집할 수 있나요?

| 화면 요소 | Unity 에디터 위치 |
|---|---|
| 메인 화면 로고·버튼·레인 아트 | `DUOVEX_UI/DesignRoot/Page_Main` |
| 곡 목록·곡 정보 | `Page_Select` |
| 플레이 HUD·레인·판정선 | `Page_Game` |
| 채보 에디터 버튼·패널 | `Page_Editor` |
| 배경 이미지 | `DUOVEX_UI/FullscreenBackdrop` 또는 `DesignRoot/Background` |
| 노트 이미지·광원·노트 두께 | `Assets/Resources/Visual/DuovexTheme.asset` |

`Assets/Resources/Visual/*.png`는 개별 스프라이트 파일로, 포토샵/그림 도구로 편집한 후 같은 이름으로 저장하거나 새로운 Sprite를 Inspector에 직접 드래그할 수 있습니다.
`Main_Orbital`, `Main_Glyph`, `Main_Scan` 모션은 **씬에 저장된 위치를 기준으로** 움직여서 Unity에서 위치를 바꿔도 실행 중 제자리로 돌아가지 않습니다.

## v0.7.2 플레이 필드 변경

- 게임 기어: 화면 기준 X=496, Y=78부터 3×96px = 288px, 508px 높이
- TAP: 각 레인 6px 좌우 여백, 최종 84×16px
- 판정선: 288×8px, 중심 Y=586
- 노트 렌더링은 저장된 Lane RectTransform을 따라가므로 커스텀 에디터 배치를 유지할 수 있음
- 메뉴·에디터·차트·효과음·스코어 로직은 v0.7.1의 동작을 유지
- 자세한 내용: `PLAYFIELD_SPECS_v0.7.2.md` 및 `CHANGELOG_v0.7.2.md`

## v0.7 기반 시각 개선

- 불투명 풀스크린 배경: Unity 하늘/3D 편집 배경 노출 방지
- 메인: 비대칭 타이포그래피, 로고 심볼, 궤도 링/레인 및 스캔 모션
- 플레이: HUD와 분리된 중앙 기어, 이번 버전에서 폭을 288px로 축소
- 노트: 새로운 화이트·블랙·DUAL, 롱노트 PNG와 명확한 대비
- 판정선: 또렷한 가로 광선 유지, 불필요한 세로 장식 없음
- 결과/곡 선택: 패널·선택 강조·타이포그래피 정리
- 실제 게임 코드를 유지해 LEVEL 1~3, 사용자 곡, 에디터 저장 방식 사용 가능

`docs/preview_menu.png`, `docs/preview_game.png`, `docs/preview_game_v071.png`는 **실제 Unity 캡처가 아닌** 이전 버전의 콘셉트 미리보기입니다. v0.7.4의 새 버튼·판정선·키 간격을 보여주는 이미지가 아니므로 실제 배치는 Unity 씬을 기준으로 확인하세요.

## 검증 범위

v0.7.3 오디오를 유지하며 v0.7.4 코드를 기반으로 업데이트했습니다. 원본 연습 채보 3개와 기존 음악 채보를 보존하고 새 채보 2개를 추가했습니다. **이 환경에서는 Unity 6.6을 실행하여 컴파일·실제 플레이 및 오디오 싱크를 검증하지 못했습니다.** `DuovexV075.unity`를 생성하여 NORMAL/HARD 시작 및 ESC 재개, 실제 판정선 싱크를 확인하세요. `LIGHT IT UP`의 00:52.4/02:14.9 드롭 싱크도 검수하세요. 문제가 있으면 Console 출력과 해당 시각을 전달해주세요.
