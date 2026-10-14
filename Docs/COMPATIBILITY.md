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

명시 규칙은 호환 작성자의 계약이므로 자동 보수 비교보다 우선합니다. 단점이나 특수 기능을 무시한 관계를 넣으면 잘못 숨겨질 수 있습니다. enabled=false로 규칙을 끌 수 있습니다. 내장 팔 규칙도 XML 패치로 비활성화할 수 있습니다.

## 2. 특수 수술 결과 연결

RecipeDef의 modExtensions에 다음을 넣을 수 있습니다.

```xml
<li Class="CanMoreImplant.ImplantRecipeExtension">
  <implant>MySpecialImplantHediff</implant>
  <exclude>false</exclude>
  <allowAutomaticComparison>false</allowAutomaticComparison>
</li>
```

implant는 addsHediff 대신 사용할 건강 상태 정의입니다. exclude=true는 이 수술을 후보 탐색에서 제외합니다. allowAutomaticComparison은 사용자 정의 Worker가 단순 교체품으로 비교 가능하다고 보증할 때만 true로 설정합니다. 수치 외 효과가 있는 경우 자동 비교가 다시 보류될 수 있습니다.

이 확장만으로 수술 없는 아이템 사용형 장착 시스템이 자동 발견되는 것은 아닙니다. 그런 시스템에는 별도 어댑터 코드가 필요합니다.

## 3. 건강 창 변경 모드

원본 HealthCardUtility.DrawHealthSummary의 탭 그리기 호출과 DrawPawnHealthCard를 사용합니다. 해당 함수를 호출하지 않는 완전 대체 건강 화면에서는 별도 호환 패치가 필요합니다. 다른 모드의 수술 목록/게임 정의/건강 상태를 변경하지 않습니다.
