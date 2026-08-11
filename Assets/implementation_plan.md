# 【Skyline-Dash】全体アーキテクチャ設計・実装ロードマップ計画

ハイスピード・空中パルクールFPS「Skyline-Dash」（目標完成: 9月中旬）の全体設計方針、オブジェクト指向・コンポーネント設計、GC対策、および段階的な実装ロードマップをまとめました。

---

## 1. 全体設計方針 (Architecture Principles)

短期間（約1.5ヶ月）で操作感（JUICE/爽快感）を極限まで磨き込むため、以下の4大原則に則って設計します。

1. **疎結合なコンポーネント設計 (Decoupled Components)**
   - C# インターフェース (`IDamageable`, `ISpeedModifier`) や C# Event/Action を活用し、各システムが直接互いを強参照しない構造にします。
   - 例: 敵撃破時に `WeaponSystem` や `Enemy` が直接 `PlayerController` を参照するのではなく、`KillBoostSystem` が撃破イベントを受信してプレイヤーへブーストを適用します。
2. **物理計算と描画・入力の完全分離 (Physics & Render Separation)**
   - **`Update()`**: Input System の入力読み取り、カメラ回転（Look）、FOV補間、VFX/アニメーション更新。
   - **`FixedUpdate()`**: Rigidbody 移動、慣性計算、レイキャスト壁検知（Wall-Run）、グラップル牽引力。
3. **徹底した Garbage Collection (GC) 抑制 (Zero-GC Mindset)**
   - **RaycastNonAlloc / SphereCastNonAlloc**: GC Alloc を発生させる `Physics.RaycastAll` を排除し、事前確保した `RaycastHit[]` 配列を再利用。
   - **Object Pooling**: 弾痕・エフェクト・ドローン敵の生成/破棄は `UnityEngine.Pool` または自作軽量プールを利用。
   - **毎フレームの Alloc 排除**: `Update` 内での `GetComponent` / `Find` / `new` / 文字列連結 / クロージャ（ラムダ式）生成の禁止。
4. **ステートパターンによる動作管理 (State Pattern for Movement)**
   - 移動状態（Grounded, Airborne, WallRun, Grapple）を巨大な `switch` 文や `if-else` のネストで管理せず、明確な State クラスまたは Handler クラスに分割して可読性と保守性を確保します。

---

## 2. ディレクトリ構成案 (Proposed Folder Structure)

`Assets/_Scripts/` 配下に目的別の命名空間（Namespace）を定義し、責任を明確にします。

```text
Assets/
└── _Scripts/
    ├── Core/
    │   ├── Interfaces/        # IDamageable.cs, IKillable.cs, ISpeedModifier.cs
    │   └── Pooling/           # ObjectPooler.cs, PooledObject.cs
    ├── Player/
    │   ├── PlayerInputHandler.cs   # Input Systemからの入力値保持
    │   ├── PlayerMotor.cs          # Rigidbodyベースの物理移動・慣性処理
    │   ├── PlayerCamera.cs         # マウスルック・FOV制御・壁走り時の傾き
    │   ├── GrappleGun.cs           # グラップルフック計算・物理牽引
    │   └── WallRunHandler.cs       # 壁走り判定および壁沿い移動物理
    ├── Weapons/
    │   ├── RaycastWeapon.cs        # 射撃・弾数・NonAllocレイキャスト
    │   └── CameraRecoil.cs         # 射撃時リコイル・スプリング復元
    ├── Combat/
    │   ├── TargetDrone.cs          # 配置型ドローン/標的（IDamageable実装）
    │   └── KillBoostSystem.cs      # 撃破イベント検知＆プレイヤー・演出へスピード付与
    └── FX/
        ├── HitEffectController.cs  # プーリングされたヒットエフェクト
        └── SpeedVFXController.cs   # 高速移動時のパーティクル・画面歪み演出
```

---

## 3. 主要コンポーネント詳細仕様

### (1) Player Component Group
- **`PlayerInputHandler`**: `InputSystem_Actions` を受け取り、`Vector2 moveInput`, `bool isJumpPressed`, `bool isGrappleHeld` などの構造体にキャッシュ。
- **`PlayerMotor`**: `Rigidbody` の速度（`linearVelocity` / `velocity`）を直接・力学的に操作。KillBoost等からの速度マルチプライヤー（`ISpeedModifier`）を毎フレーム加算・乗算。
- **`PlayerCamera`**: カメラピッチ・ヨー角のクランプ処理、壁走り時の `Z軸傾き（Tilt）` 補間、KillBoost時の `FOV Kick` (例: 75 -> 90 -> 75 SmoothStep) を統合。

### (2) WeaponSystem & Recoil
- **`RaycastWeapon`**: `Physics.RaycastNonAlloc` を用いて、射程内の `IDamageable` を検知。弾道トレイル（Bullet Trail）はプールから取得してTween/LineRenderer移動。
- **`CameraRecoil`**: `FixedUpdate` / `Update` でスプリングダンパー計算を用い、射撃時に瞬時にピッチを跳ね上げ、時間経過でスムーズに中心へ復元。

### (3) Target / Enemy & KillBoostSystem
- **`IDamageable` インターフェース**:
  ```csharp
  public interface IDamageable {
      void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
  }
  ```
- **`KillBoostSystem`**:
  - `TargetDrone.OnKilled` イベント（引数: `Vector3 position, int scorePoints` 等）をリスン。
  - イベント発火時に `PlayerMotor.ApplyKillBoost(float duration, float multiplier)` を呼び出し、同時に `SpeedVFXController` や `PlayerCamera` へ通知。

---

## 4. 段階的実装ロードマップ (Phased Implementation Roadmap)

### フェーズ 1: 物理パルクール移動の基本構築 (Phase 1)
- [ ] `PlayerInputHandler` の作成 (Unity New Input System との連動)
- [ ] `PlayerMotor` の作成 (Rigidbody による地上移動・空中慣性・ジャンプ)
- [ ] `PlayerCamera` の作成 (FPS視点、カメラルック、スムーズ制御)
- [ ] `WallRunHandler` の実装 (レイキャストによる左右の壁検知・壁沿い速度維持)
- [ ] `GrappleGun` の実装 (ワイヤーの着弾点検知・プレイヤー牽引力・慣性ジャンプの開放)

### フェーズ 2: 射撃と敵・ヒット判定システム (Phase 2)
- [ ] `IDamageable` インターフェースおよび軽量オブジェクトプールの設計
- [ ] `RaycastWeapon` の実装 (GCレス射撃・弾薬管理・レート制限)
- [ ] `TargetDrone` の実装 (耐久値、ダメージ受容、撃破イベント発火)
- [ ] `CameraRecoil` の実装 (射撃感の向上)

### フェーズ 3: スピードブースト & キル連鎖メカニクス (Phase 3)
- [ ] `KillBoostSystem` の実装 (敵撃破検知・ブースト時間のタイマー管理)
- [ ] `PlayerMotor` / `PlayerCamera` へのブースト統合 (移動速度UP & Dynamic FOV Kick)

### フェーズ 4: ポリッシュ・演出・GCチューニング (Phase 4)
- [ ] トレイル / ヒットエフェクトのプーリング化
- [ ] 高速移動時の風切りVFX / スクリーン効果
- [ ] プロファイラーによる GC Alloc 0 チェックおよび最適化

---

## 5. 開発の進め方・コードレビュー方針 (`ANTIGRAVITY.agent.md` 準拠)

1. **ステップバイステップ開発**: 一度に全コードを生成せず、各フェーズのコンポーネント単位で開発者と一緒にコードを組み上げます。
2. **コードレビュー & ハウツー解説**: 各スクリプト作成時に、良い点・改善点（GC/物理分離/責務分散）を解説し、ヒントや疑似コードから段階的に実装します。
3. **即時テストとフィードバック**: 各コンポーネント完成ごとに動作検証を行える設計にします。

---

## 検証プラン (Verification Plan)

### 自動テスト / コンパイル検証
- Unity Editor 上でのスクリプトコンパイル確認 (`Assembly-CSharp.csproj` にエラーが出ないこと)
- GC Alloc の確認 (Unity Profiler / Deep Profiling で `Update` / `FixedUpdate` 内の分配率を監視)

### 手動テスト (Unity Editor PlayMode)
- **移動**: 壁走り、グラップル後へのスムーズな慣性引き継ぎ
- **射撃**: レイキャストヒットとヒットエフェクトプーリング
- **KillBoost**: 敵撃破直後の加速感とカメラ FOV Kick の視覚的爽快感
