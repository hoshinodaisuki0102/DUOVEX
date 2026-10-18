# DUOVEX v0.7 — VISUAL OVERHAUL (Unity 6.6)

이번 버전은 v0.6.1 기반 **UI·스프라이트 전면 리디자인**입니다. 음악과 채보 데이터 형식, 판정 로직은 유지했습니다.

## 설치

**기존 프로젝트 패치**
1. Unity 에디터에서 Play를 종료하고 프로젝트 폴더를 백업합니다.
2. 패치 ZIP 안의 `Assets` 폴더를 프로젝트에 병합합니다.
3. Unity에서 컴파일 오류가 없는지 확인합니다.
4. `Tools > Duovex > Generate Editable UI Scene`을 선택합니다.
5. `Assets/Scenes/DuovexV07.unity`가 생성·열리면 Play를 누릅니다.

**새 프로젝트로 실행**
전체 프로젝트 ZIP의 `duovex-unity` 폴더를 Unity Hub에서 Add Project from Disk로 열고, 위의 3~5번을 진행합니다.

- 기존 `Assets/Scenes/DuovexEditable.unity` 및 `Main.unity`는 삭제하거나 덮어쓰지 않습니다.
- 생성기가 `DuovexV07.unity`라는 별도 파일을 만듭니다. 이미 그 이름의 씬이 있으면 덮어쓰지 않고 종료합니다.
- 생성 씬이 마음에 들면 `File > Save As...`로 자신만의 씬을 복제하여 작업해도 됩니다.
- Unity의 현재 활성 Play 씬은 에디터가 마지막으로 저장한 씬을 기준으로 하므로 Play 전 `DuovexV07.unity`를 열어두세요.

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

## v0.7 시각 개선

- 불투명 풀스크린 배경: Unity 하늘/3D 편집 배경 노출 방지
- 메인: 비대칭 타이포그래피, 로고 심볼, 궤도 링/레인 및 스캔 모션
- 플레이: 288px에서 384px로 기어 폭 증가, HUD 분리, 더 차분한 플레이 필드
- 노트: 새로운 화이트·블랙·DUAL, 롱노트 PNG와 명확한 대비
- 판정선: 또렷한 가로 광선 유지, 불필요한 세로 장식 없음
- 결과/곡 선택: 패널·선택 강조·타이포그래피 정리
- 실제 게임 코드를 유지해 LEVEL 1~3, 사용자 곡, 에디터 저장 방식 사용 가능

`docs/preview_menu.png`와 `docs/preview_game.png`는 **실제 Unity 캡처가 아니라** 제공된 스프라이트에 기반한 콘셉트 미리보기입니다. 실제 배치는 Unity의 씬이 기준입니다.

## 검증 범위

프로젝트 원본 v0.6.1을 기반으로 수정했습니다. ZIP 구조, C# 괄호·참조 패턴, PNG·JSON 기본 검사를 수행했으나 **Unity 6.6에서 컴파일/플레이를 직접 검증하지는 못했습니다.** 오류가 나면 Console 전체 내용을 전달해주세요.
