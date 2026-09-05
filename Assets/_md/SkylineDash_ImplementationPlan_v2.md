# 【Skyline-Dash】ゲーム企画・仕様 & 実装ロードマップ（v2）

※ 本ドキュメントは旧 `implementation_plan.md` を置き換える改訂版です。
新仕様（スピードレベルシステム・ステージギミック）を反映し、残り約2週間（試遊・投票イベント向け）の開発計画をまとめています。

---

## 1. 基本情報

| 項目 | 内容 |
|---|---|
| タイトル / リポジトリ | Skyline-Dash (skyline-dash) |
| プラットフォーム | PC（デスクトップFPS） |
| ジャンル | ハイスピード・空中パルクールFPS |
| 完成目標 | 9月下旬（残り約2週間 / 試遊・投票イベント用） |
| 開発エンジン | Unity（New Input System / URP 想定） |

### ゲームコンセプト
ビル群の上を駆け抜け、光の玉（Orb）を回収しながら加速を維持する。
「止まらず、食らわず、落ちず」に走り続けるほど最高速（Overdrive）へ到達できる、
タイムアタック／スコアアタック性の強いパルクールFPS。

---

## 2. コアメカニクス

### 2-1. 実装済みメカニクス（現状のコード資産）

| メカニクス | 対応スクリプト | 状態 |
|---|---|---|
| ウォールラン | `WallRunning.cs` | 実装済み |
| 基本移動 & 慣性 | `PlayerMovement.cs` | 実装済み（改善余地あり） |
| スライディング | `Sliding.cs` | 実装済み |
| 壁登り | `Climbing.cs` | 実装済み |
| カメラルック | `PlayerCam.cs` / `MoveCamera.cs` | 実装済み |

### 2-2. スピードレベルシステム（新規仕様・本作の核）

走る／集めることで蓄積する「スピードゲージ」に応じて、4段階のスピードレベルが変化する。

| レベル | 名称 | プレイヤーへの影響 | 演出 |
|---|---|---|---|
| Lv.1 | 通常 | 標準速度 | 特になし |
| Lv.2 | 加速 | 移動速度 微増 | 風切り音発生、FOVが少し広がる |
| Lv.3 | 高速 | 移動速度 増加、ワイヤー射出速度アップ | 画面端にスピードライン発生 |
| Lv.4 | 最高速 / Overdrive | ジャンプ飛距離・ウォールラン速度が最大化 | 圧倒的な疾走感、追加VFX |

### 2-3. スピードゲージの増減ルール

#### 加算（ゲージUP）
- **光の玉（Orb）の回収**
  - コース上に配置した光の玉をプレイヤーが通過するとゲージ加算。
  - コースの導線（インジケーター）を兼ねる。
- **継続走行**
  - 止まらず走り続ける、ウォールランを成功させることで、時間経過とともに徐々に蓄積。
  - 減速・停止中は加算されない（逆に微減させる選択も検討）。

#### 減算・レベルダウン
- **タレット攻撃の被弾**
  - ダメージを受けるとゲージが減少し、スピードレベルが低下。
- **落下（落水）リスポーン**
  - 海に落ちるとゲージが大幅減少（またはLv.1へリセット）。
  - 直近の足場へ即時復帰する（ペナルティはあくまで「速度」と「タイム」で、ストレスを最小化）。

#### 設計メモ
- ゲージ減少時は「即レベルダウン」ではなく「Lvダウン演出 → 0.3〜0.5秒後に適用」など、体感を調整できる余白を残す。
- レベルアップ時は `PlayerCamera` へ FOV Kick を通知し、爽快感を演出する。

---

## 3. ステージ環境 & ギミック

| 要素 | 概要 | 使用技術 |
|---|---|---|
| ビル群 | 直線・高低差のある爽快な走行ルート | ProBuilder |
| 海面 | ビル群の下に広がる一面の海。低空飛行時のスリル演出 | Shader Graph（水面シェーダー） |
| タレット | プレイヤーを自動狙撃する環境障害。障害物で遮断しつつテンポよく回避 | 自作スクリプト + 簡易モデル |
| 光の玉（Orb） | 走行ルートの導線を兼ねて配置。回収でゲージ加算 | 簡易Prefab / アセット |

### レベルデザイン方針
- 1本のメインルートを基本とし、Orbの並びで「最短・最速ライン」を示す。
- 壁走りゾーン → ジャンプ → スライド → グラップル（任意）の流れを途切れさせない。
- タレットは「回避を促す配置」に留め、射線を遮れる壁やルート分岐を用意する。

---

## 4. アーキテクチャ設計（改訂版）

### 4-1. 設計原則
1. **疎結合なコンポーネント設計**
   - C# イベント（`Action` / `UnityEvent`）で通知し、各システムが直接互いを強参照しない。

   - **演出は UnityEvent 優先**: サウンド・パーティクル・FOV・UI アニメなど「あとから調整したい」要素は、
     `[SerializeField] private UnityEvent` として公開し、シーン上の Inspector から接続・差し替えできるようにする。
     スクリプトを書き換えずに、演出の有無・差し替え・微調整ができる。
   - 例: `OrbPickup` に `[SerializeField] private UnityEvent _onOrbCollected;` を持たせ、
     Inspector で `SpeedGaugeController.AddGauge` と `SpeedSoundController.PlayOrbSound`、`SpeedVFXController.PlayOrbFx` を接続。
2. **物理計算と入力・描画の分離**
   - `Update()`: 入力読み取り、状態判定の呼び出し、FOV補間、VFX/アニメ更新。
   - `FixedUpdate()`: Rigidbody 移動、慣性計算、ウォールラン・スライド物理。
3. **GC抑制（Zero-GC Mindset）**
   - レイキャストは `Physics.Raycast`（非Alloc）または事前確保した `RaycastHit[]` + `RaycastNonAlloc` を使用。
   - Orb・弾・VFXは可能な限りプーリング。
   - `Update` 内の `GetComponent` / `Find` / `new` / 文字列連結 / クロージャ生成を禁止。
4. **ステートパターンによる状態管理**
   - 移動状態（Grounded / Air / WallRun / Slide / Climb）は `PlayerMovement` の enum を拡張するか、
     Handler クラスへ分離して管理。

### 4-2. 推奨ディレクトリ構成

```text
Assets/
└── _Scripts/
    ├── Core/
    │   ├── Interfaces/          # IDamageable.cs, ISpeedModifier.cs, ISpeedGaugeListener.cs
    │   └── Pooling/             # ObjectPooler.cs, PooledObject.cs
    ├── Player/
    │   ├── PlayerInputHandler.cs    # 入力をキャッシュして他クラスへ供給（新規）
    │   ├── PlayerMovement.cs        # 既存を速度レベル対応へ改修
    │   ├── PlayerMotor.cs           # 速度レベルと移動パラメータの統合（改修 or 新規）
    │   ├── PlayerCamera.cs          # PlayerCam.cs を改修（FOV制御を統合）
    │   ├── WallRunning.cs           # 既存（速度レベル反映）
    │   ├── Sliding.cs               # 既存
    │   ├── Climbing.cs              # 既存
    │   └── MoveCamera.cs            # 既存
    ├── Speed/
    │   ├── SpeedGaugeController.cs  # ゲージ・レベルの集中管理（新規）
    │   ├── SpeedLevelDefinition.cs  # ScriptableObjectでレベル毎のパラメータ定義（新規）
    │   └── SpeedLevelConfig.asset   # レベル設定アセット
    ├── Pickups/
    │   └── OrbPickup.cs             # 光の玉の回収判定（新規）
    ├── Enemies/
    │   ├── Turret.cs                # 索敵・射撃（新規）
    │   └── TurretProjectile.cs      # 弾（プーリング推奨）（新規）
    ├── Systems/
    │   ├── RespawnSystem.cs         # 落水検知・直近足場への即時復帰（新規）
    │   └── Checkpoint.cs            # リスポーン地点の定義（新規）
    ├── FX/
    │   ├── SpeedVFXController.cs    # 風切りエフェクト・スピードライン（新規）
    │   └── SpeedSoundController.cs  # 風切り音・Orb回収音（新規）
    └── UI/
        └── SpeedGaugeUI.cs          # ゲージ表示（新規）
```

### 4-3. 主要スクリプト仕様

#### SpeedGaugeController（新規・中心システム）
- 現在のゲージ値 `0〜1`、現在のレベル `1〜4` を保持。
- `AddGauge(float amount)` / `ReduceGauge(float amount)` を公開。
- Orb回収・継続走行・被弾・落水をイベントで受けて加減算。
- レベル変化時に `OnLevelChanged`（UnityEvent）を発火し、Inspector から FOV・VFX・速度・UI 演出を接続・差し替えできるようにする。

```csharp
using UnityEngine;
using UnityEngine.Events;

public class SpeedGaugeController : MonoBehaviour
{
    [System.Serializable]
    public class LevelChangedEvent : UnityEvent<int, int> { } // (oldLevel, newLevel)

    [SerializeField] private SpeedLevelDefinition[] _levels;
    [SerializeField] private LevelChangedEvent _onLevelChanged = new LevelChangedEvent();

    public float NormalizedGauge { get; private set; }
    public int CurrentLevel { get; private set; }

    public void AddGauge(float amount) { /* レベル閾値判定と _onLevelChanged.Invoke(oldLevel, newLevel) */ }
    public void ReduceGauge(float amount) { /* レベルダウン判定と _onLevelChanged.Invoke(oldLevel, newLevel) */ }
}
```

#### SpeedLevelDefinition（ScriptableObject）
- レベル毎に `moveSpeedMultiplier` / `jumpMultiplier` / `wallRunMultiplier` /
  `fovValue` / `wireSpeedMultiplier` などを定義し、Inspectorで調整可能にする。

#### OrbPickup（新規）
- `OnTriggerEnter` でプレイヤーを検知し、`[SerializeField] private UnityEvent _onOrbCollected;` を `Invoke` する。
- 加算・演出はスクリプトで直呼びせず、Inspector から `_onOrbCollected` に
  `SpeedGaugeController.AddGauge` / `SpeedSoundController.PlayOrbSound` / `SpeedVFXController.PlayOrbFx` を接続する。
- 回収後は非アクティブ化（プーリング or `SetActive(false)`）。

#### Turret（新規）
- `Line of Sight（LoS）` チェック：プレイヤーが射程内 & 遮蔽物なしで射撃。
- `TurretProjectile` を一定間隔で射出（プーリング推奨）。
- 被弾時は `[SerializeField] private UnityEvent _onPlayerHit;` を `Invoke` する。
  ゲージ減少・ダメージ・被弾音などは Inspector から `SpeedGaugeController.ReduceGauge` や `SpeedSoundController` へ接続する。

#### RespawnSystem（新規）
- 水面（または落下境界）をトリガーで検知。
- 事前登録した `Checkpoint` のうち直近のものを選び、プレイヤーを即時復帰。
- 復帰時は `[SerializeField] private UnityEvent _onRespawned;` を `Invoke` する。
  ゲージ大幅減算（またはリセット）・UI/サウンド演出は Inspector から `SpeedGaugeController.ReduceGauge` などへ接続する。

---

## 5. 残り2週間の開発ロードマップ

### 〜9月上旬（第1ステップ）
- [ ] `SpeedGaugeController` + `SpeedLevelDefinition` の実装
- [ ] 光の玉（Orb）の簡易Prefab作成と回収スクリプト
- [ ] タレットの仮配置と射撃スクリプト（LoS判定）
- [ ] レベルごとの速度変化（移動パラメータ・FOV変化）の紐付け
- [ ] 既存 `PlayerMovement` / `PlayerCam` への速度・FOV反映

### 9月1週目（第2ステップ）
- [ ] ProBuilderによるコース全体のレベルデザイン
- [ ] 落下・即時リスポーン処理（`RespawnSystem` + `Checkpoint`）
- [ ] 途切れずに走り抜けられるルート構築
- [ ] 壁走り・スライドなどの既存ギミックを組み込んだルート調整

### 直前調整（第3ステップ）
- [ ] UI（スピードゲージ表示）の実装
- [ ] サウンド（風切り音・Orb回収音・被弾音）の追加
- [ ] スピードライン・FOV変化などの演出調整
- [ ] 試遊用ビルド出力と動作確認
- [ ] ストレスのない操作性（リスポーン時間・カメラ感度・落下ペナルティ）の最終調整

---

## 6. 検証プラン

### 自動検証 / コンパイル検証
- Unity Editor 上でのスクリプトコンパイル確認（`Assembly-CSharp` にエラーが出ないこと）
- `SpeedGaugeController` のレベル閾値テスト（ゲージ0→1の往復で状態が壊れないこと）

### 手動テスト（Unity Editor PlayMode）
- **スピードレベル**: Orb回収と継続走行で Lv.1 → Lv.4 まで気持ちよく上がるか
- **ペナルティ**: 被弾・落水時にレベルが下がるが、復帰後のテンポが悪くないか
- **ルート**: コース全体を一度も停止せず走り切れるか
- **演出**: FOV変化・スピードライン・風切り音が速度感を損なわず強調しているか
- **GC**: Unity Profiler / Deep Profiling で `Update` / `FixedUpdate` の Alloc が概ね0であること

---

## 7. 今後の開発・コードレビュー方針（`ANTIGRAVITY.agent.md` 準拠）

1. **段階的ヒント提示**: 一度に完成コードを出さず、まず現状コードの良い点・問題点を指摘し、疑似コードやヒントから段階的に進める。
2. **Unityのベストプラクティス検証**:
   - `Update()` 内の `GetComponent` / `Find` 排除
   - 物理移動（`FixedUpdate`）と入力処理（`Update`）の分離
   - `Instantiate` / `Destroy` の抑制とプーリング
   - 神クラス化の防止（`PlayerMovement` の責務分離）
3. **既存コードのリファクタリングを並行**: 移動系スクリプトはすでに動作しているため、破壊的変更を避けつつ、速度レベル対応と責務分離を進める。

---

## 8. 変更履歴
- v2: 新仕様（スピードレベルシステム・Orb・タレット・落水リスポーン）を反映し、ロードマップとアーキテクチャを再設計。