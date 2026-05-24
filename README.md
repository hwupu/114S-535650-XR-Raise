# Gp2 XR Camp Project 3

## 資料夾結構

```
Assets/
├── Oculus/                        # Oculus XR integration
├── PolygonParticleFX/             # 第三方粒子特效素材
├── Prefabs/                       # 遊戲物件預製件
│   ├── Admission/                 # 入場相關
│   ├── Animals/                   # 動物模型（含動畫、材質）
│   ├── Coin/                      # 金幣
│   ├── Dumbbell/                  # 啞鈴
│   ├── KuaiKuai/                  # 乖乖零食
│   ├── Lays/                      # 樂事零食
│   ├── Paw/                       # 爪印
│   └── Vault/                     # 跳馬
├── Resources/                     # Meta XR 音效設定
├── Scenes/
│   └── SceneForest/               # 森林場景（Scene 1）
│       └── _External/             # 第三方 Asset Store 素材
│           ├── Free Stylized Hand-Painted Skybox/
│           ├── Handpainted_Grass_and_Ground_Textures/
│           ├── IL3DN/             # 植被著色器
│           ├── Nicrom/            # 風效著色器
│           ├── Pandazole_Ultimate_Pack/
│           ├── VegetationSpawner/
│           ├── VolumetricFog2/
│           └── Zenith - Low Poly Floating Islands/
├── Scripts/                       # 遊戲邏輯腳本
├── Settings/                      # URP 渲染管線設定
├── XR/                            # XR Plugin 設定
└── scene1_prefab/                 # 室內場景（Scene 2）素材
    ├── house/                     # Stylized House Interior HDRP（第三方，未納入 git）
    └── *.glb / *.mat              # 自製 3D 模型與材質
```

## Git LFS

大型二進位檔案透過 Git LFS 管理。追蹤的檔案類型：`.png`, `.jpg`, `.jpeg`, `.psd`, `.tga`, `.tiff`, `.exr`, `.hdr`, `.fbx`, `.obj`, `.blend`, `.wav`, `.mp3`, `.ogg`, `.mp4`, `.mov`, `.unitypackage`, `.ttf`, `.otf`, `.bin`。

After cloning, run:
```bash
git lfs install
git lfs pull
```

> **若場景中出現粉紅色物件，代表 Git LFS 尚未安裝或未執行 `git lfs pull`。**

### Windows 安裝步驟

1. 至 [https://git-lfs.com](https://git-lfs.com) 下載並執行安裝程式，或使用 winget：
   ```
   winget install GitHub.GitLFS
   ```
2. 開啟 Git Bash 或命令提示字元，執行：
   ```
   git lfs install
   git lfs pull
   ```

### macOS 安裝步驟

1. 使用 Homebrew 安裝：
   ```
   brew install git-lfs
   ```
2. 執行：
   ```
   git lfs install
   git lfs pull
   ```
