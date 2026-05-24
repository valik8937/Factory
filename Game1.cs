using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixelTexture;

    private Registry _registry;
    private WorldManager _world;
    private Camera _camera;
    private TextureManager _textureManager;
    private InputSystem _input;
    private RenderSystem _renderer;
    private UIRenderer _ui;
    private PlayerController _player;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

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
        _graphics.PreferredBackBufferWidth = GameConfig.DefaultScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConfig.DefaultScreenHeight;
        _graphics.ApplyChanges();

        _registry = new Registry();
        _world = new WorldManager();
        _camera = new Camera(Vector2.Zero);
        _input = new InputSystem();

        _player = new PlayerController();
        _player.Initialize(_registry);

        Exiting += (_, _) => _world?.Stop();
        _world.Start();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _textureManager = new TextureManager(GraphicsDevice);
        _textureManager.Load(GameConfig.TexturePath);

        _renderer = new RenderSystem(_textureManager, _pixelTexture);
        _ui = new UIRenderer(_pixelTexture);
    }

    protected override void UnloadContent()
    {
        _world.Stop();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(_camera, GraphicsDevice, GameConfig.TileSize);

        if (_input.ExitPressed)
            Exit();

        // Застосовуємо ввід гравця
        _player.ApplyInput(_registry, _input);

        // Оновлюємо позицію гравця для завантажувача чанків
        var pTransform = _registry.Transforms[_player.PlayerId];
        _world.UpdatePlayerPosition(pTransform.X, pTransform.Y, GameConfig.TileSize);

        // Рух
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MovementSystem.Update(_registry, _world, dt, GameConfig.TileSize);

        // Камера слідує за гравцем
        _camera.Follow(_player.GetCenter(_registry));

        // Будування/видалення блоків
        if (_input.ActiveLayer == 1 && _input.IsPlaceBlock)
        {
            if (_player.CanPlaceBlock(_registry, _input.TargetTileX, _input.TargetTileY, GameConfig.TileSize))
            {
                _world.SetTile(_input.TargetTileX, _input.TargetTileY, _input.ActiveLayer, 3); // 3 = brick
            }
        }
        else if (_input.IsPlaceBlock)
        {
            int tileId = _input.ActiveLayer switch
            {
                0 => 1, // grass
                2 => 4, // roof
                _ => 3  // brick
            };
            _world.SetTile(_input.TargetTileX, _input.TargetTileY, _input.ActiveLayer, tileId);
        }

        if (_input.IsRemoveBlock)
        {
            _world.SetTile(_input.TargetTileX, _input.TargetTileY, _input.ActiveLayer, 0);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 22, 33));

        var activeChunks = _world.GetActiveChunksCopy();

        _renderer.Draw(_spriteBatch, activeChunks, _registry, _player.PlayerId, _camera, GraphicsDevice, _input, GameConfig.TileSize);
        _ui.Draw(_spriteBatch, _registry, _world, _input, _player.PlayerId, GameConfig.TileSize, GraphicsDevice);

        base.Draw(gameTime);
    }
}
