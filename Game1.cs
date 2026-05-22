using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Factory;


public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private Registry _registry;
    private WorldManager _world;
    private Camera _camera;
    private int _playerId;
    private Texture2D _pixelTexture;

    private readonly float _tileSize = 32f;
    private int _activeLayer = 1; // Шар, на якому ми будуємо (0 - земля, 1 - перешкоди, 2 - дах)

    private KeyboardState _prevKeyboardState;
    private MouseState _prevMouseState;

    // Структура для сортування рендерингу (Y-sorting)
    private struct RenderItem
    {
        public float SortY;
        public Action DrawAction;
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Exiting += (sender, args) => _world?.Stop();

        // Дозволяємо користувачу розтягувати вікно
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (sender, args) =>
        {
            _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            _graphics.ApplyChanges();
        };
    }

    protected override void Initialize()
    {
        // Встановлюємо комфортну роздільну здатність екрана
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        _graphics.ApplyChanges();

        _registry = new Registry();
        _world = new WorldManager();
        _camera = new Camera(Vector2.Zero, 2.0f); // Масштаб камери 2x

        // Створюємо сутність гравця в ECS
        _playerId = _registry.CreateEntity();
        _registry.Transforms[_playerId] = new TransformComponent(0f, 0f, 1); // Початковий шар 1
        _registry.Velocities[_playerId] = new VelocityComponent(0f, 0f);
        _registry.Sprites[_playerId] = new SpriteComponent(Color.Black, 32, 32); // Чорний квадрат 32x32 (під розмір клітинки)
        _registry.Collisions[_playerId] = new CollisionComponent(32, 32, true);
        _registry.GridMovements[_playerId] = new GridMovementComponent(160f); // Швидкість покрокового руху (160 пікс/сек = 5 клітинок/сек)

        // Запуск фонового завантажувача чанків
        _world.Start();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Створюємо білу текстуру 1x1 для відмальовки кольорових блоків без використання спрайтів
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    protected override void UnloadContent()
    {
        // Зупиняємо фоновий потік при завершенні роботи
        _world.Stop();
        base.UnloadContent();
    }

    // Подія виходу обробляється через Exiting у конструкторі

    protected override void Update(GameTime gameTime)
    {
        var kState = Keyboard.GetState();
        var mState = Mouse.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kState.IsKeyDown(Keys.Escape))
            Exit();

        // 1. Керування переміщенням гравця (WASD або Стрілочки)
        float speed = 200f; // Швидкість гравця
        float vx = 0;
        float vy = 0;

        if (kState.IsKeyDown(Keys.Left) || kState.IsKeyDown(Keys.A)) vx = -speed;
        if (kState.IsKeyDown(Keys.Right) || kState.IsKeyDown(Keys.D)) vx = speed;
        if (kState.IsKeyDown(Keys.Up) || kState.IsKeyDown(Keys.W)) vy = -speed;
        if (kState.IsKeyDown(Keys.Down) || kState.IsKeyDown(Keys.S)) vy = speed;

        // Діагональне нормалізування (щоб гравець не бігав по діагоналі в 1.4 рази швидше)
        if (Math.Abs(vx) > 0.001f && Math.Abs(vy) > 0.001f)
        {
            vx *= 0.7071f;
            vy *= 0.7071f;
        }

        _registry.Velocities[_playerId] = new VelocityComponent(vx, vy);

        // 2. Зміна активного шару для будівництва (Клавіші 1, 2, 3)
        if (kState.IsKeyDown(Keys.D1) && !_prevKeyboardState.IsKeyDown(Keys.D1)) _activeLayer = 0;
        if (kState.IsKeyDown(Keys.D2) && !_prevKeyboardState.IsKeyDown(Keys.D2)) _activeLayer = 1;
        if (kState.IsKeyDown(Keys.D3) && !_prevKeyboardState.IsKeyDown(Keys.D3)) _activeLayer = 2;

        // 3. Взаємодія мишкою (ЛКМ - ставити блок, ПКМ - видаляти)
        Vector2 mouseWorld = _camera.ScreenToWorld(new Vector2(mState.X, mState.Y), GraphicsDevice);
        int tileX = (int)Math.Floor(mouseWorld.X / _tileSize);
        int tileY = (int)Math.Floor(mouseWorld.Y / _tileSize);

        // ЛКМ: Будівництво блоку
        if (mState.LeftButton == ButtonState.Pressed)
        {
            bool canPlace = true;
            
            // Якщо будуємо на шарі колізій (Layer 1), перевіряємо чи не стоїть там гравець
            if (_activeLayer == 1)
            {
                var playerTrans = _registry.Transforms[_playerId];
                var playerCol = _registry.Collisions[_playerId];
                
                Rectangle playerBox = new Rectangle((int)playerTrans.X, (int)playerTrans.Y, playerCol.Width, playerCol.Height);
                Rectangle blockBox = new Rectangle((int)(tileX * _tileSize), (int)(tileY * _tileSize), (int)_tileSize, (int)_tileSize);
                
                if (playerBox.Intersects(blockBox))
                {
                    canPlace = false; // Блок всередині гравця ставити не можна
                }
            }

            if (canPlace)
            {
                _world.SetTile(tileX, tileY, _activeLayer, 1);
            }
        }

        // ПКМ: Видалення блоку
        if (mState.RightButton == ButtonState.Pressed)
        {
            _world.SetTile(tileX, tileY, _activeLayer, 0);
        }

        // 4. Оновлення систем
        var pTransform = _registry.Transforms[_playerId];
        _world.UpdatePlayerPosition(pTransform.X, pTransform.Y, _tileSize);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MovementSystem.Update(_registry, _world, dt, _tileSize);

        // 5. Камера слідує за гравцем (з плавною інтерполяцією)
        pTransform = _registry.Transforms[_playerId];
        Vector2 playerCenter = new Vector2(pTransform.X + 16f, pTransform.Y + 16f); // Центр гравця 32x32 (+16)
        _camera.Follow(playerCenter, 0.12f); // Плавність 12% за кадр

        _prevKeyboardState = kState;
        _prevMouseState = mState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Преміальний темний фон
        GraphicsDevice.Clear(new Color(18, 22, 33));

        List<Chunk> activeChunks = _world.GetActiveChunksCopy();

        // Починаємо рендеринг світу з трансформацією камери
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp, // PointClamp для чіткого ретро піксель-арту
            null, null, null,
            _camera.GetViewMatrix(GraphicsDevice)
        );

        // === ШАР Z = 0: Земля (Трава) ===
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 0);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        DrawGroundTile(gx, gy, tileType);
                    }
                }
            }
        }

        // === ШАР Z = 1: Стіни, Об'єкти та Гравець (з сортуванням Y-sorting) ===
        List<RenderItem> layer1Items = new List<RenderItem>();

        // Збираємо блоки на шарі 1
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 1);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        
                        // Координата сортування — низ блоку
                        float sortY = (gy + 1) * _tileSize; 
                        int localGx = gx;
                        int localGy = gy;
                        
                        layer1Items.Add(new RenderItem
                        {
                            SortY = sortY,
                            DrawAction = () => DrawPurpleBlock(localGx, localGy, false)
                        });
                    }
                }
            }
        }

        // Збираємо динамічні сутності з ECS на шарі 1
        foreach (int entityId in _registry.Entities)
        {
            if (!_registry.HasTransform(entityId) || !_registry.HasSprite(entityId))
                continue;

            var transform = _registry.Transforms[entityId];
            if (transform.Z != 1) continue;

            var sprite = _registry.Sprites[entityId];
            float sortY = transform.Y + sprite.Height;

            int localId = entityId;
            var localTransform = transform;
            var localSprite = sprite;

            layer1Items.Add(new RenderItem
            {
                SortY = sortY,
                DrawAction = () => DrawEntity(localId, localTransform, localSprite)
            });
        }

        // Сортуємо за віссю Y та малюємо
        layer1Items.Sort((a, b) => a.SortY.CompareTo(b.SortY));
        foreach (var item in layer1Items)
        {
            item.DrawAction();
        }

        // === ШАР Z = 2: Дах (Верхні об'єкти, що перекривають гравця) ===
        foreach (var chunk in activeChunks)
        {
            for (int y = 0; y < Chunk.Size; y++)
            {
                for (int x = 0; x < Chunk.Size; x++)
                {
                    int tileType = chunk.GetTile(x, y, 2);
                    if (tileType > 0)
                    {
                        int gx = (chunk.ChunkX << 4) + x;
                        int gy = (chunk.ChunkY << 4) + y;
                        // Малюємо блоки шару 2 напівпрозорими, щоб бачити гравця під дахом
                        DrawPurpleBlock(gx, gy, true);
                    }
                }
            }
        }

        // Малюємо сітку таргетування мишки
        Vector2 mouseWorld = _camera.ScreenToWorld(Mouse.GetState().Position.ToVector2(), GraphicsDevice);
        int targetTx = (int)Math.Floor(mouseWorld.X / _tileSize);
        int targetTy = (int)Math.Floor(mouseWorld.Y / _tileSize);
        DrawTargetSelector(targetTx, targetTy);

        _spriteBatch.End();

        // === РЕНДЕР СТАТИЧНОГО ІНТЕРФЕЙСУ (UI) ===
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        DrawUI();

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    // Відмальовка блоку землі (Трава з плитковою структурою)
    private void DrawGroundTile(int gx, int gy, int type)
    {
        int x = (int)(gx * _tileSize);
        int y = (int)(gy * _tileSize);
        int size = (int)_tileSize;

        // Світло-зелена або темно-зелена трава
        Color color = (type == 2) ? new Color(34, 105, 54) : new Color(30, 95, 48);
        
        // Малюємо тіло тайла
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, size, size), color);
        
        // Малюємо тонкі темні рамки для відчуття піксельної сітки
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, size, 1), new Color(22, 60, 32) * 0.3f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 1, size), new Color(22, 60, 32) * 0.3f);
    }

    // Відмальовка тривимірного фіолетового блоку
    private void DrawPurpleBlock(int gx, int gy, bool isTransparent)
    {
        int x = (int)(gx * _tileSize);
        int y = (int)(gy * _tileSize);
        int size = (int)_tileSize;

        Color mainColor = new Color(110, 48, 196); // Яскравий фіолетовий
        Color topColor = new Color(153, 90, 240);  // Світла фаска зверху/зліва
        Color shadowColor = new Color(64, 23, 120); // Тінь знизу/справа

        if (isTransparent)
        {
            mainColor *= 0.55f;
            topColor *= 0.55f;
            shadowColor *= 0.55f;
        }

        // Основна частина блоку
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, size, size), mainColor);

        // Верхній та лівий світловий відблиск (Bevel)
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, size, 2), topColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 2, size), topColor);

        // Нижня та права тіні
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + size - 2, size, 2), shadowColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x + size - 2, y, 2, size), shadowColor);
    }

    // Відмальовка динамічних сутностей
    private void DrawEntity(int entityId, TransformComponent transform, SpriteComponent sprite)
    {
        int px = (int)transform.X;
        int py = (int)transform.Y;

        if (entityId == _playerId)
        {
            // Малюємо тіло чорного квадрата
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, sprite.Width, sprite.Height), new Color(24, 24, 28));

            // Біла окантовка навколо гравця
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, sprite.Width, 1), Color.White);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py + sprite.Height - 1, sprite.Width, 1), Color.White);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, 1, sprite.Height), Color.White);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px + sprite.Width - 1, py, 1, sprite.Height), Color.White);

            // Милі бірюзові очі, які світяться (підлаштовані під 32x32)
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px + 6, py + 8, 5, 5), Color.Aqua);
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px + 21, py + 8, 5, 5), Color.Aqua);
        }
        else
        {
            // Для майбутніх мобів/предметів
            _spriteBatch.Draw(_pixelTexture, new Rectangle(px, py, sprite.Width, sprite.Height), sprite.Color);
        }
    }

    // Рамка вибору під мишкою
    private void DrawTargetSelector(int tx, int ty)
    {
        int x = (int)(tx * _tileSize);
        int y = (int)(ty * _tileSize);
        int size = (int)_tileSize;

        Color frameColor = Color.Yellow * 0.7f;
        
        // Малюємо рамку вибору
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, size, 1), frameColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + size - 1, size, 1), frameColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 1, size), frameColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(x + size - 1, y, 1, size), frameColor);
    }

    // Інтерфейс користувача (Debug UI)
    private void DrawUI()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;

        // 1. Інформаційне вікно з ефектом скла (Glassmorphism)
        Rectangle panelRect = new Rectangle(16, 16, 360, 200);
        
        // Фон панелі з альфа-каналом
        _spriteBatch.Draw(_pixelTexture, panelRect, new Color(10, 14, 25, 200));
        
        // Неонова рамка
        Color borderColor = new Color(0, 191, 255, 120); // DeepSkyBlue
        _spriteBatch.Draw(_pixelTexture, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 2), borderColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(panelRect.X, panelRect.Y + panelRect.Height - 2, panelRect.Width, 2), borderColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(panelRect.X, panelRect.Y, 2, panelRect.Height), borderColor);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(panelRect.X + panelRect.Width - 2, panelRect.Y, 2, panelRect.Height), borderColor);

        // Текстова інформація
        var pTrans = _registry.Transforms[_playerId];
        int playerTileX = (int)Math.Floor(pTrans.X / _tileSize);
        int playerTileY = (int)Math.Floor(pTrans.Y / _tileSize);
        int playerChunkX = playerTileX >> 4;
        int playerChunkY = playerTileY >> 4;

        int activeChunksCount = _world.GetActiveChunksCopy().Count;

        int textX = panelRect.X + 16;
        int textY = panelRect.Y + 16;
        int lineGap = 18;

        PixelFont.DrawString(_spriteBatch, _pixelTexture, "MONOGAME 2D CHUNK ENGINE", new Vector2(textX, textY), Color.Cyan, 2);
        textY += lineGap + 4;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, $"PLAYER POS: [{(int)pTrans.X}, {(int)pTrans.Y}] (TILE: {playerTileX}, {playerTileY})", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, $"CHUNK POS : [{playerChunkX}, {playerChunkY}]", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, $"ACTIVE CHUNKS: {activeChunksCount}", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap + 4;

        // Показ активного шару для будівництва
        string layerName = _activeLayer switch
        {
            0 => "GROUND (LAYER 0) - NO COLLISION",
            1 => "OBSTACLE (LAYER 1) - SOLID BLOCK",
            2 => "ROOF (LAYER 2) - TRANSPARENT",
            _ => "UNKNOWN"
        };
        PixelFont.DrawString(_spriteBatch, _pixelTexture, $"ACTIVE LAYER: {layerName}", new Vector2(textX, textY), Color.Yellow, 1);

        // 2. Блок інструкцій керування (у нижньому лівому кутку)
        Rectangle instrRect = new Rectangle(16, GraphicsDevice.Viewport.Height - 110, 480, 94);
        _spriteBatch.Draw(_pixelTexture, instrRect, new Color(5, 7, 12, 220));
        _spriteBatch.Draw(_pixelTexture, new Rectangle(instrRect.X, instrRect.Y, instrRect.Width, 1), Color.Gray * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(instrRect.X, instrRect.Y + instrRect.Height - 1, instrRect.Width, 1), Color.Gray * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(instrRect.X, instrRect.Y, 1, instrRect.Height), Color.Gray * 0.4f);
        _spriteBatch.Draw(_pixelTexture, new Rectangle(instrRect.X + instrRect.Width - 1, instrRect.Y, 1, instrRect.Height), Color.Gray * 0.4f);

        int instX = instrRect.X + 16;
        int instY = instrRect.Y + 12;

        PixelFont.DrawString(_spriteBatch, _pixelTexture, "CONTROLS & HOW TO PLAY:", new Vector2(instX, instY), Color.Orange, 1);
        instY += lineGap;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, "- WASD / ARROWS : MOVE PLAYER (BLACK SQUARE)", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, "- LKM (LEFT CLICK)  : PLACE BLOCK ON ACTIVE LAYER", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        PixelFont.DrawString(_spriteBatch, _pixelTexture, "- PKM (RIGHT CLICK) : REMOVE BLOCK ON ACTIVE LAYER", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        // Інструкція для перемикання шарів
        PixelFont.DrawString(_spriteBatch, _pixelTexture, "- KEY 1, 2, 3       : CHANGE ACTIVE LAYER (0/1/2)", new Vector2(instX, instY), Color.DarkGray, 1);
    }
}
