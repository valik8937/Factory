using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Factory;

/// <summary>
/// Завантажує PNG-текстуру та парсить zTXt метадані (Simple Texture Editor) для отримання суб-текстур.
/// Формат метаданих: { "subTextures": [ { "name": "...", "x": ..., "y": ..., "w": ..., "h": ... } ] }
/// </summary>
public class TextureManager
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D _atlasTexture;
    private readonly Dictionary<string, SubTexture> _subTextures = new();

    public Texture2D Atlas => _atlasTexture;

    public TextureManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void Load(string path)
    {
        byte[] pngBytes = File.ReadAllBytes(path);
        _atlasTexture = Texture2D.FromStream(_graphicsDevice, new MemoryStream(pngBytes));

        string json = ExtractZtxt(pngBytes, "SimpleTextureEditor");
        if (json != null)
        {
            ParseMetadata(json);
            Console.WriteLine($"[TextureManager] Loaded {_subTextures.Count} sub-textures from {path}");
        }
        else
        {
            Console.WriteLine($"[TextureManager] Loaded {path} (no zTXt metadata found)");
        }
    }

    public Texture2D GetTexture(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!_subTextures.TryGetValue(name, out var sub))
            throw new KeyNotFoundException($"Sub-texture '{name}' not found in atlas.");

        return _atlasTexture;
    }

    public Rectangle GetSourceRect(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (!_subTextures.TryGetValue(name, out var sub))
            throw new KeyNotFoundException($"Sub-texture '{name}' not found in atlas.");

        return sub.Rect;
    }

    public bool TryGetSourceRect(string name, out Rectangle rect)
    {
        if (!string.IsNullOrEmpty(name) && _subTextures.TryGetValue(name, out var sub))
        {
            rect = sub.Rect;
            return true;
        }
        rect = default;
        return false;
    }

    public IEnumerable<string> GetTextureNames() => _subTextures.Keys;

    public void Draw(SpriteBatch sb, string name, Rectangle destination, Color color)
    {
        if (!TryGetSourceRect(name, out var srcRect))
            return;
        sb.Draw(_atlasTexture, destination, srcRect, color);
    }

    public void Draw(SpriteBatch sb, string name, Vector2 position, Color color)
    {
        if (!TryGetSourceRect(name, out var srcRect))
            return;
        sb.Draw(_atlasTexture, position, srcRect, color);
    }

    public void Draw(SpriteBatch sb, string name, Rectangle destination, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
    {
        if (!TryGetSourceRect(name, out var srcRect))
            return;
        sb.Draw(_atlasTexture, destination, srcRect, color, rotation, origin, effects, layerDepth);
    }

    private void ParseMetadata(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("subTextures", out var subTexturesElement))
            return;

        foreach (var sub in subTexturesElement.EnumerateArray())
        {
            string name = sub.GetProperty("name").GetString()!;
            int x = sub.GetProperty("x").GetInt32();
            int y = sub.GetProperty("y").GetInt32();
            int w = sub.GetProperty("w").GetInt32();
            int h = sub.GetProperty("h").GetInt32();

            _subTextures[name] = new SubTexture
            {
                Name = name,
                Rect = new Rectangle(x, y, w, h)
            };
        }
    }

    /// <summary>
    /// Сканує PNG-байти у пошуках zTXt-чанку з вказаним ключовим словом та декомпресує його (zlib/DEFLATE).
    /// </summary>
    private static string ExtractZtxt(byte[] png, string keyword)
    {
        int pos = 8; // Пропускаємо сигнатуру PNG (8 байт)

        while (pos < png.Length - 12)
        {
            int length = ReadBigEndianInt32(png, pos);
            pos += 4;

            byte[] typeBytes = new byte[4];
            Array.Copy(png, pos, typeBytes, 0, 4);
            string type = Encoding.ASCII.GetString(typeBytes);
            pos += 4;

            byte[] data = new byte[length];
            Array.Copy(png, pos, data, 0, length);
            pos += length;

            // CRC (4 байти)
            pos += 4;

            if (type == "zTXt")
            {
                // zTXt: keyword (null-terminated) + compression method (1 byte) + compressed data
                int nullIdx = Array.IndexOf(data, (byte)0);
                if (nullIdx < 0) continue;

                string kw = Encoding.Latin1.GetString(data, 0, nullIdx);
                if (kw != keyword) continue;

                // Пропускаємо compression method byte (має бути 0 = zlib/deflate)
                int compressedStart = nullIdx + 2;
                int compressedLength = data.Length - compressedStart;

                // DEFLATE-дані без zlib-заголовка? У PNG zTXt використовується zlib, 
                // але .NET DeflateStream очікує raw DEFLATE. Пропускаємо 2 байти zlib header.
                byte[] compressedData = new byte[data.Length - compressedStart];
                Array.Copy(data, compressedStart, compressedData, 0, compressedData.Length);

                return DecompressZlib(compressedData);
            }
        }

        return null;
    }

    private static string DecompressZlib(byte[] data)
    {
        // zTXt використовує zlib-формат (2 байти заголовку + DEFLATE + 4 байти Adler-32).
        // DeflateStream у .NET працює з raw DEFLATE, тому пропускаємо zlib header (2 байти).
        if (data.Length < 2)
            return null;

        // Перевіряємо zlib-заголовок: CMF (0x78) та FLG
        // Якщо це стандартний zlib — пропускаємо 2 байти
        int deflateStart = 2;
        int deflateLength = data.Length - 2 - 4; // мінус zlib header (2) та Adler-32 checksum (4)

        if (deflateLength <= 0)
            return null;

        using var compressedStream = new MemoryStream(data, deflateStart, deflateLength);
        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        deflateStream.CopyTo(resultStream);
        return Encoding.UTF8.GetString(resultStream.ToArray());
    }

    private static int ReadBigEndianInt32(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
    }

    private struct SubTexture
    {
        public string Name;
        public Rectangle Rect;
    }
}
