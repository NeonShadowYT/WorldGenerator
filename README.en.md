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

**Powerful tool for advanced prefab placement in 3D space**  
*Build complex scenes with intelligent object spawning*

<div align="center">
   
<img width="700" alt="World Generator Interface" src="https://github.com/user-attachments/assets/0c358a40-3689-45db-bd43-63897907064c" />

</div>

## ✨ What it does

<div align="center">
   
| Feature | What you get |
|---------|--------------|
| 🎯 **Smart Placement** | Auto surface detection, obstacle avoidance, ceiling/floor checks. |
| 🌳 **Clustering** | Natural object groups with adjustable density and size. |
| 🔦 **Flexible Rays** | Standard and spherical rays, customizable emission points, real-time visualization. |
| 📊 **Full Debug** | Placement statistics, ray visualization, optimization tips. |
| 🎲 **Random Variations** | Scale, rotation, vertical offset - each object is unique. |
| ⚡ **Real-time Generation** | Hit a button, see results instantly. |

</div>

<div align="center">
   
<img width="700" alt="Clustering Demo" src="https://github.com/user-attachments/assets/069db7d7-d386-4d69-bac4-2c28d8ed9e7e" />

</div>

## 🚀 Installation

<details>
<summary><strong>📦 Via .unitypackage (easiest)</strong></summary>

1. Go to [Releases](https://github.com/NeonShadowYT/WorldGenerator/releases).
2. Download the `.unitypackage` of the latest release.
3. In Unity: `Assets > Import Package > Custom Package...` → select the file → `Import`.

> Perfect if you want a stable version without Git fuss.

</details>

<details>
<summary><strong>🔗 Via Git URL (for updates)</strong></summary>

1. Open **Package Manager** (`Window > Package Manager`).
2. Click `+` → **"Add package from git URL..."**.
3. Paste: `https://github.com/NeonShadowYT/WorldGenerator.git`
4. Click **"Add"**.

> This keeps you on the bleeding edge.

</details>

<details>
<summary><strong>📁 Manual (from ZIP)</strong></summary>

1. Download the repo as ZIP (`Code > Download ZIP`).
2. Unzip it.
3. Drag the `Assets` folder into your project.
4. Restart Unity.

> Be careful not to overwrite your own files with the same names.

</details>

## 📖 How to use

### ⚡ Quick Start

1. Create an empty `GameObject` and add the `World Generation` component.
2. Add prefabs to the list and tweak parameters.
3. Hit **🎲 Generate** in the inspector.
4. Watch the process in real time.

<div align="center">
   
<img width="700" alt="Main Settings" src="https://github.com/user-attachments/assets/37f58624-ecc3-4868-b245-ec96e9243cd2" />

</div>

### ⚙️ Core Settings

| Parameter | What it does |
|-----------|--------------|
| **Population** | Target number of objects to spawn. |
| **Dimensions** | Spawn zone size (X,Z - area, Y - height check). |
| **Prefabs** | List of prefabs to choose from randomly. |
| **Scale Range** | Random scale range (0.8–1.2 = 80%–120%). |
| **Rotation Range** | Random rotation around Y axis. |
| **Vertical Offset** | Landing height adjustment. |

<div align="center">
   
<img width="700" alt="Clustering Settings" src="https://github.com/user-attachments/assets/ef19def2-7c45-4531-967e-f6eeda29ec31" />

</div>

### 🌳 Clustering

- Creates natural object groups.
- Adjustable cluster count and size.
- Minimum distance control between groups.
- Random distribution within clusters.

### 📐 Placement Rules

- **Collision Mask** - valid surface layers.
- **Avoid Mask** - obstacle layers.
- **Align To Surface** - auto-align to normal.
- **Avoidance Radius** - obstacle check radius.

### 📊 Stability Check

- **Edge Check Radius** - surface flatness check radius.
- **Max Height Difference** - allowed height variance.
- **Stability Check Rays** - number of check rays.
- **Min Success Percentage** - required success rate.

<div align="center">
   
<img width="700" alt="Debug Visualization" src="https://github.com/user-attachments/assets/49b6b490-0823-4728-abae-57e4dc57332f" />

</div>

### 🔦 Ray System

- **Ray Types** - standard and spherical.
- **Emission Points** - top, sides, volume.
- **Random Deviation** - for complex surfaces.
- **Ray Visualization** - real-time, right in the editor.

<div align="center">
   
<img width="700" alt="Debug Statistics" src="https://github.com/user-attachments/assets/18382d66-9dfc-484c-b6e1-54b17dd1a66e" />

</div>

### 📊 Debug & Statistics

- **Placement efficiency stats**.
- **Visualization of all ray types**.
- **Detailed error info**.
- **Optimization tips** for your settings.

## 🛠️ Project Structure

| Class | What it does |
|-------|--------------|
| `WorldGeneration` | Main generator component. |
| `SpawnSettings` | Generation settings container. |
| `PlacementCalculator` | Position calculation logic. |
| `IPlacementStrategy` | Placement strategy interface. |

## ⚡ Requirements

- Unity 2022.3+ (2023.1+ recommended)
- Basic knowledge of `LayerMask` in Unity

## 🐛 Troubleshooting

<details>
<summary><strong>🌱 Objects spawn in the air</strong></summary>

**Why:** Incorrect `Collision Mask` or `Vertical Offset`.

**Fix:** Make sure `Collision Mask` includes surface layers and `Vertical Offset` is adjusted to your object sizes.

</details>

<details>
<summary><strong>🧱 Objects overlap each other</strong></summary>

**Why:** `Avoidance Radius` is too small or clustering is disabled.

**Fix:** Increase `Avoidance Radius` or enable clustering with minimum distance settings.

</details>

<details>
<summary><strong>🔦 Rays are not visible</strong></summary>

**Why:** Visualization is disabled or a Debug settings error.

**Fix:** Enable `Show Rays` in debug settings and make sure generation is active.

</details>

<div align="center">
   
## ⭐ Support the Project

If this tool saved you time - drop a star on GitHub. It helps other devs find useful projects.
  
[![GitHub stars](https://img.shields.io/github/stars/NeonShadowYT/WorldGenerator?style=for-the-badge&logo=github&color=FFC107)](https://github.com/NeonShadowYT/WorldGenerator/stargazers)
[![GitHub followers](https://img.shields.io/github/followers/NeonShadowYT?style=for-the-badge&logo=github&label=Follow&color=282c34)](https://github.com/NeonShadowYT)
   
</div>

### 🔧 Other Tools of Mine

| Tool | What it does |
|------|--------------|
| [![Icon Creator](https://img.shields.io/badge/Icon%20Creator-2d2f48?style=for-the-badge&logo=github&logoColor=white)](https://github.com/NeonShadowYT/IconCreator) | Create icons from 3D models right in the editor. |
| [![Neon Imperium](https://img.shields.io/badge/Neon%20Imperium-2d2f48?style=for-the-badge&logo=github&logoColor=white)](https://github.com/NeonShadowYT/NeonImperium) | Open-source website for Starve Neon - HTML/CSS/JS, feel free to peek and reuse. |

<div align="center">

[![Starve Neon](https://raw.githubusercontent.com/NeonShadowYT/NeonShadowYT/main/images/banner-development.gif)](https://neonshadowyt.github.io/NeonImperium/starve-neon)
   
### 📸 Join the Community
  
[![YouTube](https://img.shields.io/youtube/channel/subscribers/UC2pH2qNfh2sEAeYEGs1k_Lg?style=for-the-badge&logo=youtube&logoColor=white&label=YouTube&labelColor=FF0000&color=282c34)](https://www.youtube.com/@NeonShadow-neon)
[![Twitch](https://img.shields.io/twitch/status/neoncyndows?style=for-the-badge&logo=twitch&logoColor=white&label=Twitch&labelColor=9146FF&color=282c34)](https://www.twitch.tv/neoncyndows)
[![Discord](https://img.shields.io/discord/1033727594467704842?style=for-the-badge&logo=discord&logoColor=white&label=Discord&labelColor=5865F2&color=282c34)](https://discord.com/invite/9gv5sRhk9R)
[![Telegram](https://telegram-badge.vercel.app/api/telegram-badge?channelId=@voididea&style=for-the-badge&logo=telegram&logoColor=white&label=Telegram&labelColor=26A5E4&color=282c34)](https://t.me/voididea)
  
</div>

<div align="center">
   
**Made with ❤️ for those who build worlds**

</div>
