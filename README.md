# 서현석_포트폴리오

배포: Vercel (main 브랜치에 push하면 자동으로 갱신됩니다)
예비 주소: https://gustjr2457.github.io/portfolio/

- `index.html` -> 포트폴리오 페이지
- CS_Server_ProtoType -> C# 서버, 클라이언트 구현 후 간단 연동
- Yulatro-Project -> Balatro 게임 시스템 구현(모작)

## 페이지 고칠 곳

`index.html` 한 파일에 다 들어 있습니다. 외부에서 불러오는 건 웹폰트 두 개뿐입니다.

| 바꾸고 싶은 것 | 찾을 곳 |
| --- | --- |
| 색 | `<style>` 맨 위 `:root` 의 `--paper` / `--ink` / `--navy` |
| 이름, 한 줄 소개, 연락처 | `<header class="doc">` |
| 경력 | `<section id="career">` |
| 대표 사례 | `<section id="case">` |
| 프로젝트 추가 | `<article class="proj">` 를 통째로 복사해서 붙여넣기 |
| 기술 표 | `<section id="skills">` 의 `<table>` |
| 학력, 자격증 | `<section id="education">` |
| 맺음말 | `<div class="closing">` |
| 최종 수정 날짜 | `<footer>` 의 `<time>` |

## 확인용

- 인쇄 미리보기(Ctrl+P)에 맞춰 뒀습니다. 흑백으로 뽑아도 읽힙니다.
- 620px 이하 화면에서는 경력의 기간/내용 2단이 세로로 바뀝니다.
