# Can More Implant

림월드 건강 창의 **개요 · 수술** 옆에 **임플란트** 탭을 추가하는 조회 전용 모드입니다.

> 개발 미리보기 0.3.0 — 자동 테스트 68개 통과. 실제 게임 화면·입력 및 모드 조합 검증은 아직 완료하지 않았습니다.

![Can More Implant 설명용 대표 이미지](About/Preview.png)

위 이미지는 기능을 설명하는 그림이며 실제 게임 화면 캡처가 아닙니다.

## 주요 기능

- **부위별 후보 목록**: 장착된 임플란트와 추가로 장착할 수 있는 후보를 한곳에서 확인합니다.
- **표시 구분**: 장착품은 기존 건강 항목의 색상, 미장착 후보는 회색으로 표시합니다.
- **현재 부위 효율 비교**: 자연 부위를 포함해 현재 부위보다 효율이 낮은 교체품을 숨깁니다. 다른 모드 계열의 대안은 별도로 유지합니다.
- **세 열 비교**: 부위·현재 효율 / 임플란트 / 주요 효과를 표시합니다. 반복되는 출처 제목은 표시하지 않습니다.
- **주요 효과 요약**: 교체품은 기본 부위 효율, 보조 임플란트는 게임 정보창의 효과를 표시합니다. 여러 효과는 대표 항목과 `외 N개`로 요약하고 마우스 설명에 전체 요약 항목을 표시합니다.
- **모드별 내부 비교**: 모드별 비교 계열은 화면 제목 없이 내부적으로 유지합니다. 같은 모드 안의 별도 계열도 확장 설정으로 지정할 수 있습니다.
- **전용 대상 제한**: 구울 전용 수술은 구울에게, 메카니터 전용 임플란트는 메카니터에게만 표시합니다.
- **다른 모드의 수술 검색**: 일반적인 이식 수술 정의를 읽고 후보에 포함합니다. 별도 장착 시스템은 추가 연동이 필요할 수 있습니다.
- **원래 수술 기능 유지**: 이 모드는 조회만 담당하며 수술 예약은 기존 수술 탭에서 진행합니다.

## 다운로드

[0.3.0 미리보기 배포 압축본](Dist/CanMoreImplant-0.3.0.zip)을 내려받아 압축을 해제하십시오. 실제 게임에서 검증하기 전에는 시험용 저장 사본에서 사용하십시오.

이 저장소의 공개는 Steam 창작마당 게시나 안정 버전 출시를 의미하지 않습니다.

## 지원 환경

- 림월드 **1.7**. 개발 기준: **1.7.4977 rev1248**.
- 선행 모드: **Harmony** (`brrainz.harmony`, 창작마당 2009463077)의 1.7 지원 버전.
- 한국어 / 영어. 다른 언어에서는 영어로 표시합니다.
- 1.6 이하용 실행 파일은 포함하지 않습니다. 1.7에서 .NET 실행 환경이 바뀌어 별도 빌드가 필요합니다.

## 사용법

1. `CanMoreImplant` 폴더를 게임의 `Mods` 폴더 안에 복사합니다. 폴더 바로 아래에 `About/About.xml`이 있어야 합니다.
2. 모드 목록에서 Harmony와 Can More Implant를 활성화하고 재시작합니다.
3. 정착민 선택 → 건강 → 임플란트.
4. 장착품은 기존 건강 항목의 색상으로, 미장착 후보는 회색으로 표시됩니다.
5. 항목에 마우스를 올리면 출처 수술, 현재 제한, 부족할 수 있는 재료를 볼 수 있습니다. `i` 버튼은 상세 정보를 엽니다.
6. 아래 `하위 후보 표시`를 켜면 숨겨진 후보를 다시 볼 수 있습니다.

왼쪽 효율은 부상 등을 반영한 **현재 부위 효율**, 오른쪽 교체품 효율은 **정의된 기본 효율**입니다. 정착민 전체 조작·이동 능력을 뜻하지 않습니다. 보조 효과는 장착품의 현재 단계 또는 후보의 초기 단계에서 읽습니다. 조건에 따라 달라지는 값을 확정할 수 없으면 `조건부 효과`, 해석할 수 없는 효과는 `특수 효과`로 표시합니다. 요약은 모든 사용자 정의 기능을 설명하는 것이 아니므로 상세 정보 카드도 함께 확인하십시오. 수치의 크기로 좋고 나쁨을 색칠하지 않습니다.

이 탭은 수술을 예약하지 않습니다. 실제 수술은 원래 `수술` 탭을 이용하십시오.

## 표시 규칙

- 선택 대상의 수술 정의와 부위 판정을 사용합니다. 재고가 없어도 발견된 후보는 표시합니다.
- 연구가 잠겨 있거나 재료가 없어도 후보로 남습니다. 구울·메카니터 및 수술 처리 코드의 대상 조건에 맞지 않으면 제외합니다. 회색은 '즉시 실행 가능' 표시가 아닙니다.
- 같은 부위와 같은 결과 임플란트의 수술 여러 개는 한 줄로 합치고 설명에 각 수술을 표시합니다.
- 비교 대상은 같은 실제 부위의 현재 효율입니다. 건강한 자연 부위는 보통 100%, 손상/결손은 게임 계산값을 사용합니다. 정착민 전체 조작·이동 능력과는 다른 값입니다.
- 교체 후보의 `addedPartProps.partEfficiency`가 현재 효율보다 낮으면 숨깁니다. 같은 효율은 남깁니다. 후보의 장착 후 종합 능력을 시뮬레이션하지는 않습니다.
- 장착된 교체품이 없으면 모든 계열을 자연 부위와 비교합니다. 교체품이 있으면 **같은 계열 후보 및 바닐라 후보**만 효율 비교하고 다른 모드 계열은 유지합니다.
- 바닐라와 공식 확장팩은 한 그룹, 다른 모드는 각각 별도 그룹입니다. 출처 미상은 개별 그룹으로 분리합니다.
- 특수 기능이 있어도 같은 계열의 교체품은 효율 기준으로 숨길 수 있습니다. 기능 가치를 자동 판정하는 것은 아닙니다. 별도 계열을 지정하거나 `하위 후보 표시`를 사용하십시오.
- 효율이 없는 보조 임플란트는 같은 계열의 명시 상하위 관계를 이용합니다. 판정할 수 없으면 남깁니다.
- `하위 후보 표시`는 비교 필터만 해제합니다. 구울·메카니터 등 대상 제한은 해제하지 않습니다. 이미 장착된 항목은 항상 유지합니다.

### 팔 교체 예시

아래는 생체공학 125%, 초월공학 150%, 같은 추가 모드의 불타는 팔 105%, 용암 팔 110%를 가정한 **미장착 후보**입니다. 현재 장착품은 별도로 표시됩니다.

| 현재 장착품 | 남는 후보 | 숨기는 후보 |
| --- | --- | --- |
| 생체공학 팔 | 초월공학 팔, 불타는 팔, 용암 팔 | 목재 팔, 의수 |
| 용암 팔 | 생체공학 팔, 초월공학 팔 | 목재 팔, 의수, 불타는 팔 |
| 불타는 팔 | 용암 팔, 생체공학 팔, 초월공학 팔 | 목재 팔, 의수 |

## 다른 모드와의 연동 한계

일반적인 이식 수술 정의는 자동으로 발견합니다. 그러나 '어떤 모드의 어떤 임플란트든 100% 지원'하는 것은 아닙니다.

- 수술 정의 없이 아이템 사용/능력/별도 건물로 장착하는 부품은 별도 연동이 필요합니다.
- 메카니터 제한은 설치 아이템의 메카링크 요구 조건과 수술 확장 설정에서 읽습니다. 아이템 정보도 없는 임의의 모드 전용 조건은 수술 코드 또는 호환 설정이 제공해야 합니다. 메카링크 자체는 일반 정착민에게도 허용합니다.
- 결과 건강 상태가 `addsHediff`에 없거나 사용자 정의 분류를 사용하는 수술은 `ImplantRecipeExtension`을 제공할 수 있습니다.
- 다른 모드의 부위 판정이 후보를 차단하면 억지로 건강 상태를 바꿔서 우회하지 않습니다.
- 건강 창을 완전히 교체하는 모드는 새 탭을 표시하지 않을 수 있습니다. 일반 건강 창을 호출하는 모드와는 범위가 다릅니다.
- 전체 신체에 붙는 임플란트는 전신 항목으로 묶습니다. 종족/동물은 해당 종족의 수술 정의가 있어야 후보가 나옵니다.
- 오류가 있는 개별 수술은 건너뛰고 경고를 표시합니다. 오류 원인은 게임 로그에 한 번 기록합니다.

## 저장 데이터

새로운 건강 상태·아이템·수술 예약을 생성하지 않습니다. 모드 설정 두 개만 저장합니다. 기존 저장에 추가/제거할 위험을 줄이는 구조이지만, 실제 사용 전 별도 저장 사본을 권합니다.

## 검증 상태

컴파일, 실제 설치 게임의 패치 지점, 비교 로직, 간이 대상의 후보 수집을 자동 검증했습니다.
**실제 게임을 실행한 화면·입력·저장 복구·대규모 모드 조합 테스트는 아직 완료하지 않았습니다.**
창작마당 폴더 구조는 준비되어 있으나 정식 배포 전 [수동 검증 목록](Docs/TESTING.md)을 진행하십시오.

## 개발 및 배포

- `Build.ps1`: 빌드. `-Test`를 붙이면 자동 테스트까지 실행.
- [개발 문서](Docs/DEVELOPMENT.md): 파일·함수·변수 설명 및 설계 이유.
- [호환성 문서](Docs/COMPATIBILITY.md): 다른 모드용 비교 규칙과 특수 수술 연결 예시.
- [창작마당 게시 안내](Docs/WORKSHOP.md): 게시 순서와 설명문.
- `About/Preview.png`: 도형과 글자로 만든 설명용 대표 이미지. 실제 게임 캡처가 아닙니다.

게임과 Harmony의 DLL은 배포물에 포함하지 않습니다. 실제 창작마당 게시, 게임 모드 활성화 및 저장 변경은 수행하지 않았습니다.

## 문제 보고

[Issues](https://github.com/fishbowl92/CanMoreImplant/issues)에 게임 버전, 모드 목록, 재현 순서와 관련 오류 로그를 남겨주십시오. 로그에 포함된 사용자 이름이나 개인 경로 등은 공개 전에 가려주십시오.

## English summary

Can More Implant adds an **Implants** tab beside Overview and Operations in RimWorld's health panel. Three columns show body part/current efficiency, implant, and main effect. Multiple effects are summarized with an additional count and listed in the tooltip. Repeated source-mod headings are hidden, while comparison families remain internal. Vanilla and same-family replacements below the current part efficiency are hidden; other mod families remain visible. Natural, injured and missing parts use the game's current efficiency. Equal efficiency remains visible. This numeric rule does not value special abilities.

Ghoul and mechanitor restrictions are enforced. Mechanitor requirements are read from implant-item definitions or compatibility extensions. Custom installation systems without standard metadata may still require an adapter.

Requires **RimWorld 1.7** and **Harmony**. Korean and English are included. Standard implant surgery definitions from other mods are discovered automatically, but custom installation systems may require an adapter. This is a read-only browser; schedule surgery in the original Operations tab.

**Development preview:** compilation and 68 automated checks passed. In-game UI, save/load, and mod-combination testing remain pending. The preview image is an illustration, not an in-game screenshot.

## 라이선스

현재 재사용 라이선스는 지정하지 않았습니다. 저장소가 공개되어 있다는 사실만으로 재배포·수정물 배포 권한을 부여하는 것은 아닙니다. 게임과 타 모드의 권리는 각 권리자에게 있습니다.
