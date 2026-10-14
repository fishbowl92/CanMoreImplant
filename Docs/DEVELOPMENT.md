# 개발 내역 및 구조

## 작업 범위

기존 약물 프로젝트를 변경하지 않고 별도 CanMoreImplant 모드를 만들었습니다. 기존 파일/함수/변수 삭제는 없습니다. 사용자에게 전달하는 폴더에는 게임 실행 파일이나 역컴파일 소스를 포함하지 않습니다.

실제 설치된 림월드 1.7.4977의 함수를 확인한 뒤 C# 소스를 작성했습니다. .NET 10 SDK(Software Development Kit, 소프트웨어 개발 도구 모음)로 빌드합니다. Harmony는 실행 시 게임 함수 일부에 우리 코드를 연결하는 선행 모드입니다.

## 처리 순서

1. 기존 건강 개요에서 탭을 그리는 지점에 임플란트 탭을 추가합니다.
2. 선택하면 기존 창 내부에 별도의 목록을 그립니다. 개요/수술로 돌아가면 원래 게임 코드가 다시 그립니다.
3. 대상 종족의 RecipeDef(제작·수술 정의)에서 이식 수술을 찾습니다.
4. 수술 Worker(수술별 처리 클래스)가 돌려주는 실제 부위에 후보를 연결합니다.
5. Hediff(개별 건강 상태)를 조회해 현재 장착품과 후보를 합칩니다.
6. 같은 부위에 장착된 상위품이 확인될 때만 하위 후보를 숨깁니다.
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
