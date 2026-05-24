using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

/// <summary>
/// Рендеринг статичного UI (інформаційна панель, інструкції).
/// </summary>
public class UIRenderer
{
    private readonly Texture2D _pixel;

    public UIRenderer(Texture2D pixel)
    {
        _pixel = pixel;
    }

    public void Draw(SpriteBatch sb, Registry registry, WorldManager world, InputSystem input, int playerId, float tileSize, GraphicsDevice graphicsDevice)
    {
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        DrawInfoPanel(sb, registry, world, input, playerId, tileSize);
        DrawControlsPanel(sb, graphicsDevice);

        sb.End();
    }

    private void DrawInfoPanel(SpriteBatch sb, Registry registry, WorldManager world, InputSystem input, int playerId, float tileSize)
    {
        var panelRect = new Rectangle(16, 16, 360, 200);

        // Скляний фон
        sb.Draw(_pixel, panelRect, new Color(10, 14, 25, 200));

        // Неонова рамка
        Color borderColor = new Color(0, 191, 255, 120);
        sb.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 2), borderColor);
        sb.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Y + panelRect.Height - 2, panelRect.Width, 2), borderColor);
        sb.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Y, 2, panelRect.Height), borderColor);
        sb.Draw(_pixel, new Rectangle(panelRect.X + panelRect.Width - 2, panelRect.Y, 2, panelRect.Height), borderColor);

        var pTrans = registry.Transforms[playerId];
        int playerTileX = (int)Math.Floor(pTrans.X / tileSize);
        int playerTileY = (int)Math.Floor(pTrans.Y / tileSize);
        int playerChunkX = playerTileX >> 4;
        int playerChunkY = playerTileY >> 4;
        int activeChunksCount = world.GetActiveChunksCopy().Count;

        int textX = panelRect.X + 16;
        int textY = panelRect.Y + 16;
        int lineGap = 18;

        PixelFont.DrawString(sb, _pixel, "MONOGAME 2D CHUNK ENGINE", new Vector2(textX, textY), Color.Cyan, 2);
        textY += lineGap + 4;
        PixelFont.DrawString(sb, _pixel, $"PLAYER POS: [{(int)pTrans.X}, {(int)pTrans.Y}] (TILE: {playerTileX}, {playerTileY})", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap;
        PixelFont.DrawString(sb, _pixel, $"CHUNK POS : [{playerChunkX}, {playerChunkY}]", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap;
        PixelFont.DrawString(sb, _pixel, $"ACTIVE CHUNKS: {activeChunksCount}", new Vector2(textX, textY), Color.White, 1);
        textY += lineGap + 4;
        PixelFont.DrawString(sb, _pixel, $"ACTIVE LAYER: {input.GetActiveLayerName()}", new Vector2(textX, textY), Color.Yellow, 1);
    }

    private void DrawControlsPanel(SpriteBatch sb, GraphicsDevice graphicsDevice)
    {
        var instrRect = new Rectangle(16, graphicsDevice.Viewport.Height - 110, 480, 94);
        sb.Draw(_pixel, instrRect, new Color(5, 7, 12, 220));
        sb.Draw(_pixel, new Rectangle(instrRect.X, instrRect.Y, instrRect.Width, 1), Color.Gray * 0.4f);
        sb.Draw(_pixel, new Rectangle(instrRect.X, instrRect.Y + instrRect.Height - 1, instrRect.Width, 1), Color.Gray * 0.4f);
        sb.Draw(_pixel, new Rectangle(instrRect.X, instrRect.Y, 1, instrRect.Height), Color.Gray * 0.4f);
        sb.Draw(_pixel, new Rectangle(instrRect.X + instrRect.Width - 1, instrRect.Y, 1, instrRect.Height), Color.Gray * 0.4f);

        int instX = instrRect.X + 16;
        int instY = instrRect.Y + 12;
        int lineGap = 18;

        PixelFont.DrawString(sb, _pixel, "CONTROLS & HOW TO PLAY:", new Vector2(instX, instY), Color.Orange, 1);
        instY += lineGap;
        PixelFont.DrawString(sb, _pixel, "- WASD / ARROWS : MOVE PLAYER", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        PixelFont.DrawString(sb, _pixel, "- LKM (LEFT CLICK)  : PLACE BLOCK ON ACTIVE LAYER", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        PixelFont.DrawString(sb, _pixel, "- PKM (RIGHT CLICK) : REMOVE BLOCK ON ACTIVE LAYER", new Vector2(instX, instY), Color.DarkGray, 1);
        instY += lineGap;
        PixelFont.DrawString(sb, _pixel, "- KEY 1, 2, 3       : CHANGE ACTIVE LAYER (0/1/2)", new Vector2(instX, instY), Color.DarkGray, 1);
    }
}
