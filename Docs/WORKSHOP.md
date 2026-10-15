# 창작마당 업로드 안내

## 업로드 전

1. TESTING.md의 게임 내 확인 항목을 진행합니다. 컴파일 성공만으로 화면 작동까지 확인된 것은 아닙니다.
2. About/About.xml의 author를 실제 게시자 이름으로 수정합니다.
3. 게임 Mods 폴더에 CanMoreImplant 폴더를 복사합니다. 공개용 압축본은 About, Defs, Languages, 1.7, Docs와 LoadFolders.xml 등을 포함하고 빌드 임시 폴더는 제외합니다.
4. 다른 사람이 수정본을 재배포할 수 있는지 등 소스 이용 조건은 게시자가 정합니다. 이 작업에서 임의의 오픈소스 라이선스를 부여하지 않았습니다.

## 게시

Steam판 림월드의 모드 목록에서 로컬 모드를 선택하고 창작마당 업로드 기능을 이용합니다. 최초에는 비공개/친구 공개로 확인한 뒤 공개하는 것을 권장합니다. 게임에서 생성하는 PublishedFileId.txt는 이후 업데이트 식별용이므로 보관하십시오. 이 패키지에는 다른 게시물의 ID를 넣지 않았습니다.

## 소개문과 대표 이미지

[WORKSHOP_DESCRIPTION.txt](WORKSHOP_DESCRIPTION.txt)의 내용을 그대로 복사하면 됩니다. 영어 → 한국어 순서이며 About/About.xml의 소개문과 같습니다.

대표 이미지 About/Preview.png는 사용자가 제공한 실제 게임 화면입니다. Tools/New-Preview.ps1은 이전 시안 생성 도구이므로 현재 대표 이미지를 유지하려면 실행하지 마십시오.

## 배포에 넣으면 안 되는 파일

- 게임의 Assembly-CSharp.dll, UnityStubs.dll, Waylith DLL
- 0Harmony.dll (별도 선행 모드로 설치)
- Source/obj, Tests/bin, Tests/obj
- 개인 저장 파일, 로그, 개인 설정

코드는 사용자가 수정할 수 있도록 프로젝트 폴더에 별도로 포함했습니다. 압축 배포본은 실행용 파일 중심으로 구성합니다.
