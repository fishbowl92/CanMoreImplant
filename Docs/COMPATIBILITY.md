# 호환 확장

## 1. 명시적 상하위 관계

`Defs` 안의 XML에 다음 정의를 추가할 수 있습니다. 이름과 효율로는 판별할 수 없지만 기능적으로 확실한 관계만 선언하십시오.

```xml
<Defs>
  <CanMoreImplant.ImplantComparisonDef>
    <defName>MyMod_LowToHighEye</defName>
    <lower>MyLowEyeHediff</lower>
    <higher>MyHighEyeHediff</higher>
    <bodyPart>Eye</bodyPart>
  </CanMoreImplant.ImplantComparisonDef>
</Defs>
```

lower/higher는 아이템 이름이 아닌 HediffDef(장착 후 건강 상태 정의)입니다. bodyPart를 생략하면 같은 실제 부위라는 조건 아래 모든 부위에 적용됩니다. A→B, B→C는 A→C로 연결됩니다. A→B와 B→A가 동시에 성립하면 둘 다 숨기지 않습니다.

0.2.0부터 숫자 효율이 있는 교체품은 현재 부위 효율 기준이 우선합니다. 명시 규칙은 효율이 없는 같은 계열 보조 임플란트의 상하위 판정에 사용합니다. 단점이나 특수 기능을 무시한 관계를 넣으면 잘못 숨겨질 수 있습니다. enabled=false로 규칙을 끌 수 있습니다. 서로 다른 계열을 명시 규칙만으로 연결하지 않으며, 필요한 경우 아래 comparisonGroup을 동일하게 지정하십시오.

## 2. 특수 수술 결과 연결

RecipeDef의 modExtensions에 다음을 넣을 수 있습니다.

```xml
<li Class="CanMoreImplant.ImplantRecipeExtension">
  <implant>MySpecialImplantHediff</implant>
  <exclude>false</exclude>
  <allowAutomaticComparison>false</allowAutomaticComparison>
  <requiresMechanitor>false</requiresMechanitor>
</li>
```

implant는 addsHediff 대신 사용할 건강 상태 정의입니다. exclude=true는 이 수술을 후보 탐색에서 제외합니다. requiresMechanitor=true는 메카링크가 있는 대상만 허용합니다. 설치 아이템의 CompProperties_Usable.userMustHaveHediff가 MechlinkImplant인 경우는 별도 설정 없이 결과 임플란트에 같은 제한을 적용합니다.

allowAutomaticComparison은 기존 보수 비교 경로의 허용 선언이며 하위 호환용으로 유지합니다. 0.2.0의 수치 교체품 필터는 이 값과 관계없이 계열 및 현재 부위 효율 기준을 사용합니다. 구울 제한은 RecipeDef.mutantPrerequisite/mutantBlacklist, 다른 대상 제한은 Worker.AvailableOnNow를 사용합니다. 연구 및 재고는 별도로 확인하며 후보 필터로 사용하지 않습니다. 다른 모드가 AvailableOnNow 안에서 재고/연구까지 직접 거절하면 그 모드의 반환값을 존중합니다.

이 확장만으로 수술 없는 아이템 사용형 장착 시스템이 자동 발견되는 것은 아닙니다. 그런 시스템에는 별도 어댑터 코드가 필요합니다.

## 3. 건강 창 변경 모드

원본 HealthCardUtility.DrawHealthSummary의 탭 그리기 호출과 DrawPawnHealthCard를 사용합니다. 해당 함수를 호출하지 않는 완전 대체 건강 화면에서는 별도 호환 패치가 필요합니다. 다른 모드의 수술 목록/게임 정의/건강 상태를 변경하지 않습니다.

## 4. 비교 계열 지정

기본 계열은 HediffDef.modContentPack의 출처입니다. Core와 공식 확장팩은 하나, 외부 모드는 패키지 식별자별로 하나입니다. 다른 모드가 기존 정의를 패치하더라도 원래 정의 출처가 바뀌는 것은 아닙니다. 출처 없는 동적 정의는 오분류를 피하려고 각각 분리합니다.

한 모드에 용도가 다른 팔이 있다면 각 HediffDef.modExtensions에 다음을 넣어 계열을 분리합니다.

```xml
<li Class="CanMoreImplant.ImplantGroupExtension">
  <comparisonGroup>my.mod.fireArms</comparisonGroup>
  <groupLabel>화염 팔 계열</groupLabel>
</li>
```

comparisonGroup이 같은 정의끼리 같은 계열로 취급합니다. 충돌 방지를 위해 모드 패키지 식별자를 접두사로 사용하십시오. groupLabel은 0.2.0의 제목용 설정이며 0.3.0에서는 반복 제목을 없앴으므로 목록에 표시하지 않습니다. 필드는 기존 호환 설정을 읽을 수 있도록 유지합니다. 이 설정은 출처를 덮어쓰지 않으며 비교 계열만 지정합니다.

## 5. 주요 효과 요약

교체품의 addedPartProps.partEfficiency를 우선하고, 보조 효과는 HediffStatsUtility.SpecialDisplayStats를 통해 게임이 사용하는 명칭·단위·부호로 표시합니다. 그중 게임의 표시 우선순위가 높은 항목을 대표로 사용합니다. 높은 숫자가 좋은 효과라는 의미는 아닙니다.

장착품은 현재 CurStage, 후보는 initialSeverity에 해당하는 단계입니다. 후보의 중첩/대상 능력 배율처럼 고정 수치로 확정할 수 없는 경우 조건부 효과로 안내합니다. 모든 컴포넌트·능력·사용자 정의 로직을 추정하지는 않으므로 상세 설명 및 원래 정보 카드도 제공하며, 알 수 없는 항목은 특수 효과로 남깁니다.

같은 계열의 교체품은 특수 기능의 유무와 무관하게 `addedPartProps.partEfficiency`로 비교합니다. 여러 능력의 장단점을 하나의 등급으로 환산하지 않습니다. 효율이 낮지만 특수 기능이 필요한 부품은 별도 계열로 분리하거나 하위 후보 표시를 켜십시오. 새로운 부품의 장착 후 효과·단계·상호작용을 시뮬레이션하지 않으므로 동적 부위 효율 모드는 추가 연동이 필요할 수 있습니다.
