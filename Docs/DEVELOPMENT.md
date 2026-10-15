# 개발 내역 및 구조

## 작업 범위

기존 약물 프로젝트를 변경하지 않고 별도 CanMoreImplant 모드를 만들었습니다. 기존 파일/함수/변수 삭제는 없습니다. 사용자에게 전달하는 폴더에는 게임 실행 파일이나 역컴파일 소스를 포함하지 않습니다.

실제 설치된 림월드 1.7.4977의 함수를 확인한 뒤 C# 소스를 작성했습니다. .NET 10 SDK(Software Development Kit, 소프트웨어 개발 도구 모음)로 빌드합니다. Harmony는 실행 시 게임 함수 일부에 우리 코드를 연결하는 선행 모드입니다.

## 처리 순서

1. 기존 건강 개요에서 탭을 그리는 지점에 임플란트 탭을 추가합니다.
2. 선택하면 기존 창 내부에 별도의 목록을 그립니다. 개요/수술로 돌아가면 원래 게임 코드가 다시 그립니다.
3. 대상 종족의 RecipeDef(제작·수술 정의)에서 이식 수술을 찾습니다.
4. 구울/메카니터 및 수술 Worker(수술별 처리 클래스)의 대상 조건을 검사한 뒤 실제 부위에 후보를 연결합니다.
5. Hediff(개별 건강 상태)를 조회해 현재 장착품과 후보를 합칩니다.
6. 현재 부위 효율과 후보 계열을 비교해 하위 후보를 숨기고 출처 모드/명시 계열로 묶습니다.
7. 게임 설정과 건강 정보는 읽기만 합니다. 이 모드 자체의 표시 설정만 저장합니다.

## 생성한 코드 파일·함수·매개변수

### Source/ModEntry.cs

- `ImplantSettings.ExposeData()` — 표시 설정 직렬화. 건강 데이터가 아닌 설정만 보관하기 위해 생성.
- `CanMoreImplantMod(content)` — content는 모드 콘텐츠 정보. 설정을 읽고 Harmony 패치를 설치.
- `SettingsCategory()` — 게임 설정 목록의 모드 이름 반환.
- `DoSettingsWindowContents(rect)` — rect 영역에 설정 두 개와 주의사항 표시. 변경 후 후보 캐시 무효화.
- 변수 `hideInferior`: 하위 후보 숨김 여부. `useExplicitRules`: 명시 호환 규칙 활성화. `Settings`: 모드 설정 접근점.

### Source/HealthTabPatch.cs

- `SelectVanilla(operations)` — operations=true면 수술, false면 개요로 복귀. 원래 게임의 탭 상태를 맞춤.
- `SelectImplants()` — 새 탭을 활성화하고 목록 갱신 요청.
- `Transpiler(instructions)` — instructions는 게임 함수의 중간 명령 목록. 탭 렌더링 호출 한 곳만 교체.
- `DrawTabs(rect, tabs, maxTabWidth)` — 기존 탭 목록 뒤에 임플란트를 붙여 원본 탭 렌더러 호출.
- `Prefix(outRect, pawn, allowOperations)` — 새 탭이 선택된 경우만 새 화면을 그림. 오류 시 기존 건강 창으로 복구.
- 변수 `Active`: 현재 새 탭 선택 상태. `operationTab`: 원본 수술 탭 상태 필드. 저장 파일에 넣지 않음.

### Source/Catalog.cs

- `Invalidate()` — 다음 조회에서 목록을 다시 만들도록 요청.
- `Get(pawn)` — 대상 또는 게임이 바뀌거나 1초가 지나면 새 목록 생성. 매 화면 프레임마다 모든 수술을 검사하지 않기 위함.
- `Build(pawn)` — 현재 장착품 수집, 종족별 후보 탐색, 수술 중복 병합, 하위 판정, 부위별 정렬.
- `IsImplantDefinition(def)` — def가 이식물·의체 건강 상태인지 판별. 일반 질병이 목록에 섞이지 않도록 생성.
- `IsInstalledImplant(hediff)` — 실제 보이는 장착 건강 상태인지 확인.
- `ImplantRow.Label` — 장착품이면 실제 건강 항목 이름, 후보면 정의 이름.
- 행 변수 `Part`, `Implant`, `Installed`, `Recipes`, `Inferior`, `SimpleWorker`: 실제 부위, 건강 정의, 장착 인스턴스, 수술 경로들, 하위 여부, 단순 비교 허용 여부.
- 스냅샷 변수 `Rows`, `FailedRecipes`: 목록과 검사에 실패한 수술 수.
- 캐시 변수 `cachedPawn`, `cachedGame`, `cached`, `nextRefresh`: 대상/게임/목록/다음 갱신 시각. `warned`: 같은 오류의 로그 반복 방지.

### Source/Comparison.cs

- `ImplantComparisonDef.ConfigErrors()` — 명시 규칙의 누락 참조와 자기 자신 관계 검증.
- `IsInferior(candidate, installed, part, simpleWorker)` — 후보가 같은 부위 장착품보다 하위인지 판정. 규칙 충돌은 숨기지 않음.
- `ExplicitPath(from, to, part)` — 명시 규칙의 연결 경로 확인. 방문 기록으로 순환 탐색 방지.
- `SimpleDominates(upper, lower)` — 단순 의체의 효율만 우수하고 다른 작동 데이터가 동등한지 확인.
- `IsSimple(def)` — 사용자 정의 클래스/컴포넌트/효과 단계/확장 데이터가 없는 단순 교체품인지 판별.
- `SameValue(a, b, depth)` — 알려진 값·목록·곡선의 동등성 검사. 재귀 깊이 제한. 임의 객체는 동작을 추정하지 않음.
- 규칙 변수 `lower`, `higher`, `bodyPart`, `enabled`: 하위·상위 건강 정의, 제한 부위, 활성화 여부.
- 확장 변수 `implant`, `exclude`, `allowAutomaticComparison`: 특수 수술 결과, 제외, 단순 비교 허용 선언.
- `simpleCache`: 정의 쌍별 비교 캐시. `cosmeticFields`: 설명/표시 등의 비성능 차이 목록.

### Source/ImplantTabView.cs

- `Draw(outer, pawn, allowOperations)` — 창, 탭, 하위 표시 선택, 오류 안내, 목록을 그림. 색/폰트/정렬을 finally에서 복원.
- `DrawRows(viewport, pawn, rows)` — 스크롤 목록. 부위 이름은 그룹 첫 행에만 표시하며 긴 이름은 높이를 계산해 줄바꿈.
- `PartLabel(part)` — 부위 또는 전신 이름을 얻음.
- `Tooltip(pawn, row)` — 마우스가 올라간 항목의 수술 출처·조건·재료·기술 안내.
- 변수 `scroll`, `lastPawn`, `lastGame`: 스크롤과 대상 전환 관리. `candidateColor`: 미장착 후보 회색.

### Source/AssemblyInfo.cs

테스트 프로그램에만 내부 함수 접근을 허용합니다. 사용자 게임에 새로운 기능이나 데이터를 등록하지 않습니다.

### Tests/Program.cs

- `Main(args)`: 게임/Harmony 경로를 받아 테스트 실행. 설치된 라이브러리는 읽기만 함.
- `Run()`: 비교 규칙과 실제 게임 패치 지점 확인.
- `Part(name, efficiency)`: 가상 테스트용 의체 정의 생성.
- `Check(condition, message)`: 조건 확인, 실패 시 종료 코드 1로 중단.
- `CheckCatalog()`: 두 팔을 가진 간이 대상의 후보 병합·숨김·데이터 불변 확인.
- `FakeInstaller.GetPartsToApplyOn(pawn, recipe)`: 테스트 전용 고정 부위 수술.
- 변수 `passed`, `gamePath`, `harmonyPath`: 통과 개수 및 설치 파일 경로. 테스트 코드는 게임용 DLL에 포함되지 않음.

### 실행 및 배포 스크립트

- `Build.ps1`: GamePath/HarmonyPath 참조로 Release 빌드. Test 스위치가 있으면 테스트까지 실행.
- `Tools/Validate.ps1`: GamePath의 정의 이름과 배포 XML, 번역 키, 필수 파일 검사.
- `Tools/Package.ps1`: OutputFile 경로에 실행용 압축본 생성. 기존 파일을 덮어쓰지 않음.
- `Tools/New-Preview.ps1`: 시스템 글꼴과 도형만으로 설명용 대표 이미지 생성.

## 빌드 명령

PowerShell에서 모드 폴더를 연 뒤 실행합니다.

```powershell
.\Build.ps1 -Test
```

설치된 게임 라이브러리를 참조해 모드를 다시 컴파일한 뒤 자동 테스트를 실행합니다. 게임 설치 위치가 다르면 `-GamePath`와 `-HarmonyPath`를 지정합니다. 외부 NuGet 패키지는 다운로드하지 않습니다.

```powershell
.\Tools\Validate.ps1
.\Tools\Package.ps1
```

첫 명령은 번역과 배포 구조를 검사합니다. 두 번째는 Dist에 실행용 압축본을 만듭니다. 이미 같은 이름의 압축본이 있으면 중단하므로, 새 배포본에는 `-OutputFile`로 다른 파일명을 지정하십시오.

## 한계와 향후 검증

컴파일 성공과 자동 테스트 통과는 실제 게임 화면 검증을 대체하지 않습니다. 동적으로 효과를 추가하는 모드, 아이템 사용형 이식, 건강 창 전체 대체 모드는 별도 연동이 필요할 수 있습니다. 가격/기술 수준/이름을 이용한 무리한 자동 등급화는 구현하지 않았습니다.

## 0.2.0 변경 개요 — 2026-10-15

목적은 자연 부위를 포함한 효율 필터, 전용 대상 제한, 모드별 별도 계열 비교입니다. 파일·함수·변수 삭제는 없습니다. 게임 건강 데이터와 수술 예약은 여전히 변경하지 않습니다. 다음 항목은 위 최초 구현 설명을 보완하며, 변경된 판정 순서는 아래를 따릅니다.

### 추가한 파일·함수·변수

| 파일 | 함수/속성 및 매개변수 | 추가 이유 |
| --- | --- | --- |
| Source/ImplantGroups.cs | `Key(implant)` | 건강 정의의 명시 계열 또는 출처로 비교 그룹 식별자 반환. 출처 미상은 개별 분리 |
| Source/ImplantGroups.cs | `Label(implant)` | 그룹의 표시 제목 반환 |
| Source/TargetEligibility.cs | `Allows(pawn, recipe, implant)` | 대상, 수술, 결과 건강 정의를 받아 변이체/메카니터 제한 판정 |
| Source/TargetEligibility.cs | `MechanitorImplants` | 설치 아이템 정의에서 메카링크를 요구하는 결과 건강 정의를 수집·보관 |
| Source/TargetEligibility.cs | `Invalidate()` | 테스트 또는 정의 갱신 시 전용 임플란트 검색 캐시 무효화 |
| Source/Comparison.cs | `ShouldHide(candidate, replacement, part, currentEfficiency, simpleWorker)` | 후보, 현재 교체품, 실제 부위, 현재 효율, 기존 보수 비교 허용 여부로 새 필터 판정 |
| Source/Catalog.cs | `ImplantRow.GroupKey`, `GroupLabel` | 행의 비교 식별자와 화면 제목을 같은 규칙으로 공급 |
| Tests/Program.cs | `FakeMod(id, official)` | 파일을 마운트하지 않고 출처 메타데이터만 가진 테스트용 모드 생성 |
| Tests/Program.cs | `CheckGroupsAndEfficiency()` | 사용자 팔 예시, 동률, 계열 예외, 비정상 수치 검증 |
| Tests/Program.cs | `CheckTargetEligibility()` | 일반인/구울/다른 변이체/메카니터 조건 검증 |
| Tests/Program.cs | `BlockedInstaller.AvailableOnNow(thing, part)`, `GetPartsToApplyOn(pawn, recipe)` | 부위는 반환하지만 대상은 거절하는 수술을 재현 |

추가 변수와 용도:

- `ImplantGroupExtension.comparisonGroup`, `groupLabel`: HediffDef 확장 설정. 계열 식별자와 화면 제목.
- `ImplantGroups.Vanilla`: 공식 콘텐츠 그룹 식별자.
- `TargetEligibility.mechanitorImplants`: 검색 결과 캐시. 수술마다 모든 아이템을 재검색하지 않음.
- `ImplantRecipeExtension.requiresMechanitor`: 아이템 정보가 없는 모드 수술의 메카니터 요구 선언.
- `Build` 지역 변수 `replacement`, `currentEfficiency`, `efficiencies`: 현재 장착된 교체품, 실제 부위 효율, 부위별 계산 캐시. 계산 실패 시 숫자 아님(NaN)으로 판정 보류.
- `DrawRows` 지역 변수 `groupHeights`: 계열 제목 줄바꿈 높이. 전체 스크롤 높이에 포함하여 글자 겹침 방지.

### 변경한 기존 함수

- `ImplantCatalog.Build(pawn)`: `Allows`와 `AvailableOnNow`를 통과한 경로만 수집합니다. 기존 장착 항목은 대상 조건이 달라져도 보존합니다. 현재 효율은 게임의 `PawnCapacityUtility.CalculatePartEfficiency`로 계산하고, 부위 → 장착 여부 → 그룹 → 이름 순으로 정렬합니다.
- `ImplantTabView.DrawRows(viewport, pawn, rows)`: 부위 안에서 그룹 제목을 추가하고 제목 높이도 스크롤 계산에 반영합니다.
- `CheckCatalog()`: 자연 부위·손상·결손과 수술 대상 거절을 추가 검증합니다. 테스트 전용 객체 초기화에만 내부 필드 접근을 사용하며 실제 저장이나 게임 월드는 수정하지 않습니다.
- `IsInferior(...)` 및 기존 비교 함수는 삭제하지 않았습니다. 효율이 없는 같은 계열 임플란트의 명시 관계 및 기존 비교 경로를 유지합니다. 수치 교체품은 `ShouldHide`의 효율 판정이 우선합니다.

### 비교 우선순위

1. 장착품은 숨기지 않습니다. 전신 항목은 단일 부위 효율로 비교하지 않습니다.
2. 자연 부위면 모든 계열을 현재 효율과 비교합니다.
3. 교체품이 있으면 바닐라 후보 또는 같은 계열 후보만 효율 비교합니다. 다른 모드 계열은 보존합니다.
4. 교체 후보의 정의 효율이 현재 부위 효율보다 낮을 때만 숨깁니다. 동률 및 비정상 수치는 유지합니다.
5. 효율이 없는 보조 임플란트는 같은 계열의 명시 상하위 관계를 사용합니다.

문서·번역·버전을 0.2.0에 맞추고 `Tools/Package.ps1`의 기본 출력명을 변경했습니다. 기존 0.1.0 압축본은 삭제하거나 덮어쓰지 않습니다. 실제 화면 검증은 TESTING.md의 추가 항목이 남아 있습니다.

## 0.3.0 변경 개요 — 2026-10-15

목적은 반복 출처 제목을 없애고 실제 비교에 필요한 수치를 세 열에 표시하는 것입니다. 비교·전용 대상 제한 로직은 유지합니다. 새 파일은 `Source/ImplantEffects.cs`이며 파일/기존 함수 삭제는 없습니다.

### 추가 함수·매개변수 및 이유

| 파일 | 함수/속성 | 역할과 이유 |
| --- | --- | --- |
| Source/ImplantEffects.cs | `Build(def, installed, readStats = null)` | 건강 정의와 실제 장착 상태로 표시용 효과를 생성. 선택적 readStats 공급자는 독립 테스트에서 게임 설정 초기화를 피하기 위한 경계이며 게임에서는 원본 정보창 함수를 사용 |
| Source/ImplantEffects.cs | `SelectStage(def, installed)` | 장착품 현재 단계와 후보 초기 단계를 구분하여 최고 단계 값을 오표시하지 않음 |
| Source/ImplantEffects.cs | `HasUnresolvedScaling(stage, installed)` | 중첩·대상 능력 배율로 고정값을 확정할 수 없는지 확인 |
| Source/ImplantEffects.cs | `Summarize(entries)`, `Summary`, `Details` | 첫 효과 + 추가 개수 및 마우스 설명용 전체 요약 목록 제공 |
| Source/ImplantTabView.cs | `ColumnWidths(width, out part, out implant, out effect)` | 정보 버튼 공간을 제외하고 25/40/35 비율로 너비 배분 |
| Source/ImplantTabView.cs | `PartWithEfficiency(part, efficiencies)` | 부위 이름과 현재 효율 표시. 전신에 임의 효율을 붙이지 않고 계산 실패는 물음표로 표시 |
| Tests/Program.cs | `CheckEffectDisplay()` | 효율/단위/단계 선택/조건부 효과/추가 개수/세 열 너비 검증 |

추가/변경 변수:

- `ImplantEffects.Entries`: 게임에서 읽은 표시 항목 목록. 교체품 효율이 먼저 오고 나머지는 게임 표시 우선순위를 따름.
- `ImplantRow.effects`, `Effects`: 한 스냅샷에서 효과 문자열을 재사용. 기존 1초 목록 갱신 시 함께 재생성.
- `ImplantSnapshot.PartEfficiencies`: 필터 계산값을 화면에서도 재사용. 기존 `Build`의 지역 변수 `efficiencies`를 이 사전으로 대체.
- `DrawRows`의 `effectWidth`, `effectX`, `headings`, `headerHeight`, `parts`, `effects`: 효과 열 좌표, 고정 제목 및 줄바꿈 계산용 데이터.
- **삭제한 지역 변수 `groupHeights`**: 반복 그룹 제목 렌더링을 없앴으므로 더 이상 높이를 확보하지 않음. GroupKey/GroupLabel 및 확장 설정은 하위 호환을 위해 유지.

기존 함수 변경:

- `Build(pawn)`: 장착품만 있는 부위도 실제 효율을 계산하지만 장착품은 숨기지 않음.
- `DrawRows(viewport, pawn, rows, efficiencies)`: 효율 사전 매개변수 추가. 세 열의 글자 높이를 모두 고려하고 제목은 스크롤 밖에 고정.
- `Tooltip(pawn, row)`: 장착 여부와 관계없이 효과 요약 목록과 수치 기준 설명 추가. 원래 정보 카드 및 수술 조건 설명은 유지.

통증 효과 검사에는 원본 게임의 통증 항목 포맷터를 사용하지만 게임 설정 로딩 전 구간만 열거합니다. 나머지 단위/조건/게임 정보창 통합 동작은 실제 게임 검증이 필요합니다. 테스트가 사용자 게임 설정이나 저장 파일을 읽거나 바꾸지 않도록 최소 언어 데이터를 메모리에만 준비합니다.

0.3.0은 68개 자동 검사 및 번역/배포 검증을 수행했습니다. 0.1.0/0.2.0 압축본은 삭제하지 않고 새 압축본을 생성합니다.

## 0.3.1 변경 개요 — 2026-10-15

- About/Preview.png: 기존 설명용 그림을 사용자 제공 게임 화면으로 교체. 원본 크기·내용 유지.
- About/About.xml, README.md: 영어 → 한국어 순서의 짧은 소개문으로 정리. 긴 소개가 기능 설명을 가리지 않도록 변경.
- Docs/USAGE.md: 기존 상세 사용 안내와 호환 주의사항을 옮겨 보존.
- Docs/WORKSHOP_DESCRIPTION.txt: 창작마당에 복사할 소개문 생성. About의 설명과 동일.
- Source/CanMoreImplant.csproj: 버전만 0.3.1로 변경. Tools/Package.ps1: 기본 배포 파일명 갱신.
- 새로 만들거나 삭제한 C# 함수·매개변수·변수는 없습니다. 기존 버전 압축본은 삭제하지 않습니다.
