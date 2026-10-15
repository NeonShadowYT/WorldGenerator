<div align="center">
  <a href="README.md"><img src="https://img.shields.io/badge/🇷🇺%20Русский-2d2f48?style=for-the-badge&logoColor=white" alt="Русский"></a>
  <a href="README.en.md"><img src="https://img.shields.io/badge/🇬🇧%20English-2d2f48?style=for-the-badge&logoColor=white" alt="English"></a>
</div>

## 🌍 World Generator for Unity

<div align="center">
   
[![GitHub stars](https://img.shields.io/github/stars/NeonShadowYT/WorldGenerator?style=for-the-badge&logo=github&color=FFC107)](https://github.com/NeonShadowYT/WorldGenerator/stargazers)
[![GitHub followers](https://img.shields.io/github/followers/NeonShadowYT?style=for-the-badge&logo=github&label=Follow&color=282c34)](https://github.com/NeonShadowYT)
[![GitHub last commit](https://img.shields.io/github/last-commit/NeonShadowYT/WorldGenerator?style=for-the-badge&logo=github&color=blueviolet)](https://github.com/NeonShadowYT/WorldGenerator/commits/main)
[![GitHub release](https://img.shields.io/github/v/release/NeonShadowYT/WorldGenerator?style=for-the-badge)](https://github.com/NeonShadowYT/WorldGenerator/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3+-2296F3.svg?style=for-the-badge&logo=unity)](https://unity.com/)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

</div>

**Мощный инструмент для продвинутого размещения префабов в 3D-пространстве**  
*Создавай сложные сцены с интеллектуальной генерацией*

<div align="center">
   
<img width="700" alt="World Generator Interface" src="https://github.com/user-attachments/assets/0c358a40-3689-45db-bd43-63897907064c" />

</div>

## ✨ Что умеет?

<div align="center">
   
| Фича | Что даёт |
|------|----------|
| 🎯 **Умное размещение** | Автообнаружение поверхностей, избегание препятствий, проверка потолков/полов. |
| 📐 **Диапазон углов поверхности** | Спавн строго на полу, стенах или потолках — настраивается парой min/max. |
| 🌳 **Кластеризация** | Естественные группы объектов с настраиваемой плотностью и размером. |
| 🔦 **Гибкие лучи** | Обычные и сферические лучи, настраиваемые точки испускания, визуализация. |
| 📊 **Полная отладка** | Статистика эффективности, визуализация лучей, советы по оптимизации. |
| 🎲 **Случайные вариации** | Масштаб, вращение, вертикальный офсет - каждый объект уникален. |
| ⚡ **Реалтайм генерация** | Нажал кнопку - сразу видишь результат. |

</div>

<div align="center">
   
<img width="700" alt="Clustering Demo" src="https://github.com/user-attachments/assets/069db7d7-d386-4d69-bac4-2c28d8ed9e7e" />

</div>

## 🚀 Установка

<details>
<summary><strong>📦 Через .unitypackage (самый простой)</strong></summary>

1. Заходи в [Releases](https://github.com/NeonShadowYT/WorldGenerator/releases).
2. Качаешь `.unitypackage` последней версии.
3. В Unity: `Assets > Import Package > Custom Package...` → выбираешь файл → `Import`.

> Отличный вариант, если хочешь стабильную версию и не париться с гитом.

</details>

<details>
<summary><strong>🔗 Через Git URL (для обновлений)</strong></summary>

1. В Unity открываешь **Package Manager** (`Window > Package Manager`).
2. Нажимаешь `+` → **"Add package from git URL..."**.
3. Вставляешь: `https://github.com/NeonShadowYT/WorldGenerator.git`
4. Жмёшь **"Add"**.

> Так всегда будешь на самой свежей версии.

</details>

<details>
<summary><strong>📁 Ручная установка (из ZIP)</strong></summary>

1. Качаешь репозиторий как ZIP (`Code > Download ZIP`).
2. Разархивируешь.
3. Папку `Assets` из архива перетаскиваешь в свой проект.
4. Перезапускаешь Unity.

> Смотри, чтобы не перезаписать свои файлы с такими же именами.

</details>

## 📖 Как пользоваться?

### ⚡ Быстрый старт

1. Создай пустой `GameObject` и добавь компонент `World Generation`.
2. Добавь префабы в список и настрой параметры.
3. Нажми **🎲 Сгенерировать** в инспекторе.
4. Наблюдай за процессом в реальном времени.

<div align="center">
   
<img width="700" alt="Main Settings" src="https://github.com/user-attachments/assets/37f58624-ecc3-4868-b245-ec96e9243cd2" />

</div>

### ⚙️ Основные настройки

| Параметр | Что делает |
|----------|------------|
| **Population** | Целевое количество объектов для спавна. |
| **Dimensions** | Размеры зоны спавна (X,Z - площадь, Y - высота проверки). |
| **Prefabs** | Список префабов для случайного выбора. |
| **Scale Range** | Диапазон случайного масштаба (0.8–1.2 = 80%–120%). |
| **Rotation Range** | Случайное вращение вокруг оси Y. |
| **Vertical Offset** | Корректировка высоты посадки. |
| **Surface Angle Range** | Диапазон допустимых углов наклона поверхности (X — минимум, Y — максимум). |

<div align="center">
   
<img width="700" alt="Clustering Settings" src="https://github.com/user-attachments/assets/ef19def2-7c45-4531-967e-f6eeda29ec31" />

</div>

### 🌳 Кластеризация

- Создаёт естественные группы объектов.
- Настраиваемое количество и размер кластеров.
- Контроль минимального расстояния между группами.
- Случайное распределение внутри кластеров.

### 📐 Правила размещения

- **Collision Mask** - слои валидных поверхностей.
- **Avoid Mask** - слои препятствий.
- **Align To Surface** - автовыравнивание по нормали.
- **Avoidance Radius** - радиус проверки препятствий.
- **Surface Angle Range** - диапазон допустимых углов наклона поверхности (градусы, X = min, Y = max). Y = 0 — проверка отключена.

#### 🎯 Пресеты углов поверхности

| Цель спавна | Surface Angle Range | Что спавнится |
|-------------|---------------------|---------------|
| Полы, пологие склоны | `(0, 45)` | Деревья, кусты, камни, трава |
| Стены и сильные уклоны | `(60, 90)` | Факелы, картины, гнёзда |
| Потолки | `(150, 180)` | Пещерные сталактиты, лампы, лианы |
| Любая поверхность | `(0, 180)` | Универсальный декор |
| Только горизонталь (строго) | `(0, 5)` | Ковры, платформы, тайлы |

> Угол считается от вертикали: **0° = пол**, **90° = стена**, **180° = потолок**.

### 📊 Проверка стабильности

- **Edge Check Radius** - радиус проверки ровности поверхности.
- **Max Height Difference** - допустимая разница высот.
- **Stability Check Rays** - количество проверочных лучей.
- **Min Success Percentage** - требуемый процент успешных проверок.

<div align="center">
   
<img width="700" alt="Debug Visualization" src="https://github.com/user-attachments/assets/49b6b490-0823-4728-abae-57e4dc57332f" />

</div>

### 🔦 Система лучей

- **Типы лучей** - обычные и сферические.
- **Точки испускания** - верх, боковые стороны, объём.
- **Случайное отклонение** - для сложных поверхностей.
- **Визуализация лучей** - в реальном времени, прямо в редакторе.

<div align="center">
   
<img width="700" alt="Debug Statistics" src="https://github.com/user-attachments/assets/18382d66-9dfc-484c-b6e1-54b17dd1a66e" />

</div>

### 📊 Отладка и статистика

- **Статистика эффективности** размещения.
- **Визуализация всех типов лучей**.
- **Детальная информация об ошибках**.
- **Советы по оптимизации** настроек.

## 🛠️ Архитектура проекта

```
Assets/NeonImperium/WorldGenerations/
├── Runtime/
│   ├── Core/
│   │   ├── WorldGeneration.cs               # MonoBehaviour-фасад, Editor + Runtime пайплайны
│   │   ├── GenerationSettings.cs            # SpawnSettings + DebugRaySettings
│   │   ├── PlacementData.cs                 # Structs/enums: PlacementData, PlacementPoint, DebugRay
│   │   ├── GenerationSnapshot.cs            # Save/load слепок (FNV-1a + JsonUtility)
│   │   └── WorldGenerationExtension.cs      # Базовый класс user-расширений
│   │
│   ├── Interfaces/
│   │   ├── IPlacementValidator.cs           # Single + batch валидация
│   │   ├── IPointGenerator.cs               # Стратегия генерации кандидатов
│   │   ├── IPhysicsService.cs               # Абстракция физики (тестируемость)
│   │   ├── IDebugLogger.cs                  # Абстракция логгера лучей
│   │   └── ISpawnZone.cs                    # + BoxSpawnZone, PolygonSpawnZone, SpawnZoneBuilder
│   │
│   ├── Placement/
│   │   ├── PlacementValidator.cs            # Ray/Sphere-cast, все проверки, batch-путь
│   │   ├── PlacementJobs.cs                 # Persistent NativeArray<RaycastCommand>
│   │   ├── PointGenerator.cs                # RandomPointGenerator + ClusterPointGenerator
│   │   ├── SpatialHash.cs                   # O(1) проверка minDistance (3×3×3 ячеек)
│   │   └── PolygonTriangulator.cs           # Ear-clipping, IsConvex — без аллокаций
│   │
│   ├── Zones/
│   │   ├── SpawnZoneTypes.cs                # BoxSpawnZoneSettings, PolygonVertex, PolygonSpawnZoneSettings
│   │   └── SpawnZoneDefaults.cs             # Editor-only демо-зоны (Hexagon + Triangle)
│   │
│   ├── Services/
│   │   ├── PhysicsService.cs                # IPhysicsService → Physics.*
│   │   ├── DebugLogger.cs                   # Кольцевой буфер DebugRay (256+)
│   │   ├── ObjectInstantiator.cs            # Батчинг по 20/кадр, reuse-existing
│   │   ├── RuntimeSpawnQueueService.cs      # Awaitable-очередь с приоритетами
│   │   ├── NavMeshRegenerationService.cs    # Reflection к NavMeshSurface (без hard-dep)
│   │   └── RuntimeNavMeshBuilder.cs         # NavMeshBuilder.CollectSources + UpdateNavMeshDataAsync
│   │
│   └── Utils/
│       ├── FastRandom.cs                    # Xorshift128 PRNG, bias-free int/float
│       └── MinMaxRangeAttributes.cs         # [MinMaxRange], [MinMaxRangeInt]
│
└── Editor/
    ├── WorldGenerationView.cs               # CustomEditor, OnSceneGUI, RegenerateAll
    ├── SectionDrawer.cs                     # 8 foldout-секций инспектора
    ├── ActionButtonsDrawer.cs               # Generate/Clear/Preview/RegenerateAll
    ├── DebugInfoDrawer.cs                   # Статистика, причины отказов, советы
    ├── EditorStyleManager.cs                # GUIStyle + цветной header-texture
    ├── EditorFoldoutState.cs                # Static-состояние foldout'ов
    ├── EditorGenerationDriver.cs            # EditorApplication.update-тик
    ├── SpawnZoneSceneEditor.cs              # Handles: move/size/insert/delete + snap
    ├── MinMaxRangeDrawers.cs                # Кастомные PropertyDrawer
    └── WorldGenerationGizmos.cs             # Gizmos: зоны, лучи, кластеры
```

### 🧩 Ключевые подсистемы

| Подсистема | Что делает |
|------------|------------|
| **PlacementValidator** | Ray/Sphere-cast по поверхностям, проверки угла, устойчивости, потолка, пола, avoidance, NavMesh. Single + Batch (RaycastCommand) пути. |
| **PointGenerator** | Две стратегии: `RandomPointGenerator` (равномерно по зоне) и `ClusterPointGenerator` (кластеры с проверкой поверхностей через батч-джоб). |
| **SpatialHash** | Пространственный хэш для O(1) проверки `minDistanceBetweenObjects`. Работает с `math.hash(int3)` или FNV-fallback. |
| **PlacementJobs** | Persistent `NativeArray<RaycastCommand>` — без аллокаций в горячем цикле, авто-fallback на CPU при Sphere-кастах. |
| **SpawnZoneBuilder** | Строит список зон: Box (main + extraBoxes) или Polygon (невыпуклые 3D-объёмы). |
| **PolygonTriangulator** | Ear-clipping + выпуклость за O(n²), стек-аллокация при n ≤ 64. |
| **ObjectInstantiator** | Батчинг по 20 объектов/кадр, `Awaitable.NextFrameAsync`, режим reuseExisting (SetActive вместо Destroy). |
| **RuntimeSpawnQueueService** | Глобальная очередь `WorldGeneration` с сортировкой по `priority`. |
| **NavMeshRegenerationService** | Reflection → `Unity.AI.Navigation.NavMeshSurface.BuildNavMesh()`; работает без hard-dependency. |
| **RuntimeNavMeshBuilder** | `NavMeshBuilder.UpdateNavMeshDataAsync` — не блокирует главный поток. |
| **FastRandom** | Детерминированный Xorshift128: ~5× быстрее `System.Random`, без боксинга. |
| **GenerationSnapshot** | FNV-1a хэш зон + JsonUtility сериализация для save/load и воспроизводимости. |

### ⚙️ Runtime-пайплайн

```
Awake / Generate()
  → EnsureModulesInitialized()
  → GeneratePlacementPointsBatch()
        ├─ CanUseJobValidation ? Job-путь (RaycastCommand.ScheduleBatch)
        └─ иначе CPU-путь (Validate + SpatialHash.Insert)
  → CreateObjectsFromPlacementData()  ← ObjectInstantiator.InstantiateBatchAsync
  → TryRuntimeNavMeshRegenerationAsync()
  → CallGenerationCompleteExtensions()
  → OnGenerationComplete?.Invoke()
```

### 🎨 Editor-пайплайн

```
OnInspectorGUI
  → SectionDrawer × 8          (все настройки, foldout'ы)
  → ActionButtonsDrawer        (Generate / Preview / RegenerateAll)
  → DebugInfoDrawer            (статистика + советы)

OnSceneGUI
  → SpawnZoneSceneEditor.DrawForSpawner()
        • Move центр зоны (FreeMoveHandle)
        • Move вершины полигона
        • Slider вертикального размера
        • Insert / Delete (Ctrl-снап)
  → DrawPreview()              (до 500 сфер: зелёные/красные)

EditorGenerationDriver
  → EditorApplication.update → WorldGeneration.EditorTick()
  → SceneView.duringSceneGui → Repaint при генерации
```

## ⚡ Требования

- Unity 2022.3+ (рекомендуется 2023.1+)
- Базовые знания `LayerMask` в Unity

## 🐛 Проблемы и решения

<details>
<summary><strong>🌱 Объекты спавнятся в воздухе</strong></summary>

**Почему:** Неправильно настроен `Collision Mask` или `Vertical Offset`.

**Как починить:** Проверь, что `Collision Mask` включает слои поверхности, а `Vertical Offset` скорректирован под размер объектов.

</details>

<details>
<summary><strong>🧱 Объекты накладываются друг на друга</strong></summary>

**Почему:** Слишком маленький `Avoidance Radius` или отключена кластеризация.

**Как починить:** Увеличь `Avoidance Radius` или включи кластеризацию с настройкой минимального расстояния.

</details>

<details>
<summary><strong>📐 Объекты спавнятся под неправильным углом</strong></summary>

**Почему:** `Surface Angle Range` не соответствует рельефу сцены.

**Как починить:**
- Полы, склоны до 45° → поставь `(0, 45)`.
- Стены и обрывы → `(60, 90)`.
- Потолки, навесы → `(150, 180)`.
- Хочешь любую поверхность → `(0, 180)`.
- Отключить проверку → установи максимум `0`.

Открой `📊 Дебаг информацию` — в причинах отказов появится `📐 Угол поверхности вне диапазона`, если настройка мешает.

</details>
<details>
<summary><strong>🔦 Лучи не видны</strong></summary>

**Почему:** Отключена визуализация или ошибка в настройках `Debug`.

**Как починить:** Включи `Show Rays` в настройках отладки и проверь, что генерация активна.

</details>

<div align="center">
   
## ⭐ Поддержи проект

Если тебе пригодился этот инструмент - поставь звёздочку на GitHub. Это помогает другим разработчикам находить полезные проекты.
  
[![GitHub stars](https://img.shields.io/github/stars/NeonShadowYT/WorldGenerator?style=for-the-badge&logo=github&color=FFC107)](https://github.com/NeonShadowYT/WorldGenerator/stargazers)
[![GitHub followers](https://img.shields.io/github/followers/NeonShadowYT?style=for-the-badge&logo=github&label=Follow&color=282c34)](https://github.com/NeonShadowYT)
   
</div>

### 🔧 Другие мои инструменты

| Инструмент | Что делает |
|------------|------------|
| [![Icon Creator](https://img.shields.io/badge/Icon%20Creator-2d2f48?style=for-the-badge&logo=github&logoColor=white)](https://github.com/NeonShadowYT/IconCreator) | Создание иконок из 3D‑моделей прямо в редакторе. |
| [![Neon Imperium](https://img.shields.io/badge/Neon%20Imperium-2d2f48?style=for-the-badge&logo=github&logoColor=white)](https://github.com/NeonShadowYT/NeonImperium) | Открытый сайт для Starve Neon - HTML/CSS/JS, можно изучать и использовать. |

<div align="center">

[![Starve Neon](https://raw.githubusercontent.com/NeonShadowYT/NeonShadowYT/main/images/banner-development.gif)](https://neonshadowyt.github.io/NeonImperium/starve-neon)

### 📸 Присоединяйся к сообществу
  
[![YouTube](https://img.shields.io/badge/YouTube-FF0000?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/@NeonShadow-neon)
[![Twitch](https://img.shields.io/twitch/status/neoncyndows?style=for-the-badge&logo=twitch&logoColor=white&label=Twitch&labelColor=9146FF&color=282c34)](https://www.twitch.tv/neoncyndows)
[![Discord](https://img.shields.io/discord/1033727594467704842?style=for-the-badge&logo=discord&logoColor=white&label=Discord&labelColor=5865F2&color=282c34)](https://discord.com/invite/9gv5sRhk9R)
[![Telegram](https://telegram-badge.vercel.app/api/telegram-badge?channelId=@voididea&style=for-the-badge&logo=telegram&logoColor=white&label=Telegram&labelColor=26A5E4&color=282c34)](https://t.me/voididea)
  
</div>

<div align="center">
   
**Создано с ❤️ для тех, кто строит миры**

</div>
