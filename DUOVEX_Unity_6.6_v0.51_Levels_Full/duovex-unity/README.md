# DUOVEX v0.5 — Unity 6.6

3레인·4키(D/F/J/K) 리듬게임. WHITE / BLACK / DUAL, 양손 독립 판정, 유지 틱 롱노트 지원.

## 실행

1. Unity Hub에서 이 폴더를 Unity 6.6으로 연다.
2. `Assets/Scenes/Main.unity`를 열고 ▶ Play, **Game** 탭을 선택한다.
3. 메인 메뉴의 PLAY는 곡 선택, CHART STUDIO는 편집할 곡 선택 화면을 연다.

## 내장 레벨

PLAY에서 LV.1 TUTORIAL / LV.2 INTERLOCK / LV.3 OVERDRIVE를 골라 플레이할 수 있다. 모두 무음 맵이며 기본 채보는 수정되지 않는다. 선택한 레벨은 RETRY에서도 유지된다. LOCAL CHART와 가져온 곡은 목록 뒤에 표시한다.

## 음악과 채보를 게임 안에서 추가하기

1. PLAY 또는 CHART STUDIO → **ADD SONG**.
2. MUSIC / DOWNLOADS / HOME 폴더에서 OGG·WAV·MP3 선택(또는 경로를 붙여넣고 `IMPORT PATH`).
3. 게임이 음악 파일을 사용자 저장소의 `Songs/<곡 ID>/audio.확장자`로 **복사**하고, 빈 `chart.json`을 만든다.
4. 자동으로 열리는 CHART STUDIO에서 BPM·OFFSET 설정, 노트 배치 후 **SAVE** (`Ctrl+S`).
5. **EXIT** 후 PLAY → 추가한 곡 선택 → PLAY.

해당 폴더의 `song.json`, `chart.json`, `audio.*`가 곡 한 개를 구성한다. 게임 재실행 후에도 목록이 유지된다. 메인 메뉴에는 복잡한 설명 텍스트를 넣지 않았다.

저장소 루트는 Unity의 `Application.persistentDataPath`이며 보통 Windows에서 `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>/Songs` 아래에 있다. Unity가 설정한 Company/Product 이름에 따라 경로가 달라진다.

- 음원은 250MB 이하, OGG/WAV/MP3 지원 시도. 로컬 코덱/플랫폼에 따라 MP3 디코딩이 실패할 수 있으므로 그 경우 OGG 또는 WAV로 변환해 사용한다.
- 튜토리얼은 무음 내장 채보로 그대로 제공되고 수정되지 않는다.
- 기존 `DualSyncChart.json` 파일은 LOCAL CHART로 계속 사용한다. `Assets/Resources/song.ogg`가 있으면 LOCAL CHART에서 재생된다.
- 음악을 삭제하거나 관리하려면 사용자 데이터의 해당 Songs 폴더를 게임이 실행 중이지 않을 때 관리한다.

## 플레이

- `D F J K` 입력 / `Space` 일시정지 / `Enter` 재시작 / `Esc` 돌아가기
- F3/F4 노트 스크롤 속도 0.5x~3.0x (판정 및 음악 속도 불변)
- PERFECT ±50ms, GREAT/GOOD/BAD 색상 구분, FAST/SLOW 별도 표시. PERFECT에는 세부 판정 없음.
- HOLD는 시작과 유지 틱 판정, 떼는 순간 판정 없음.

## 에디터

- 1~7 도구; 레인 클릭 배치, HOLD 위로 드래그, 우클릭 삭제
- 마우스 휠 이동, Ctrl+휠 확대·축소
- Shift+클릭 다중 선택, Ctrl+A/C/V, Delete, Ctrl+Z/Y, Ctrl+S/O
- AUDIO 탭 파형·IN/OUT·LOOP; TIMING 탭 BPM·OFFSET·ZOOM; EDIT 탭 편집
- 상단 PLAY/TEST/SAVE/REWIND/EXIT

## v0.45 → v0.5 패치

Play 모드 종료 및 프로젝트 백업 후 **패치 ZIP의 `Assets/Scripts` 전체**와 **`Packages/manifest.json`**을 프로젝트에 병합한다. 기존 Resources, Scene, 사용자 채보는 삭제하지 않는다.

Unity 에디터에서 실제 컴파일/오디오 코덱 구동은 이 환경에서 검증하지 못했다. 첫 실행 시 Console 오류를 확인한다.
