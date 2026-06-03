# Sommoje — 코딩 규칙

## 제어 흐름 (필수)
- **얼리 리턴(guard clause)을 쓴다.** 조건이 안 맞으면 일찍 `return`해서 본문 들여쓰기를 얕게 유지한다. `if (ok) { ...긴 본문... }` 대신 `if (!ok) return; ...본문...`.
- **if문을 중복해서 쓰지 않는다.** 같은(또는 사실상 같은) 조건을 여러 곳에서 반복 검사하지 말 것. 한 번에 합치거나, 한 곳에서 가드하고 이후엔 가정한다. 중첩 `if` 안에 또 같은 검사를 넣지 않는다.

### 예시
```csharp
// 나쁨: 중첩 + 조건 중복
void Use(Unit u) {
    if (u != null) {
        if (u.alive) {
            if (u.alive && u.energy > 0) { Cast(u); }
        }
    }
}

// 좋음: 얼리 리턴 + 조건 한 번
void Use(Unit u) {
    if (u == null || !u.alive) return;
    if (u.energy <= 0) return;
    Cast(u);
}
```

## 일반
- 주변 코드의 스타일/네이밍/주석 밀도에 맞춘다.
- 네임스페이스: 2D는 `Sommoje.Battle`, 3D는 `Sommoje.Action3D`. (`Sommoje.Grid`는 `UnityEngine.Grid`와 충돌하니 금지)
- 새 인풋시스템 대신 레거시 `Input` 사용(현재 프로젝트 설정).
