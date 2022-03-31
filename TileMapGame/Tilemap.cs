using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace TileMapGame
{
    public class Tilemap
    {

        int _tileWidth, _tileHeight, _mapWidth, _mapHeight;

        Texture2D _tileSetTexture;

        Rectangle[] _tiles;

        int[] _map;

        string _filename;

        public Tilemap(string filename)
        {
            _filename = filename;
        }

        public void LoadContent(ContentManager content)
        {
            string data = File.ReadAllText(Path.Join(content.RootDirectory, _filename));
            var lines = data.Split('\n');

            var tilesetFilename = lines[0].Trim();
            _tileSetTexture = content.Load<Texture2D>(tilesetFilename);


            var secondLine = lines[1].Split(',');
            _tileWidth = int.Parse(secondLine[0]);
            _tileHeight = int.Parse(secondLine[1]);

            int tilesetColumns = _tileSetTexture.Width / _tileWidth;
            int tilesetRows = _tileSetTexture.Height / _tileHeight;
            _tiles = new Rectangle[tilesetColumns * tilesetRows];

            for (int y = 0; y < tilesetColumns; y++)
            {
                for (int x = 0; x < tilesetRows; x++)
                {
                    int index = y * tilesetColumns + x;
                    _tiles[index] = new Rectangle(x * _tileWidth, y * _tileHeight, _tileWidth, _tileHeight);
                }
            }

            var thirdLine = lines[2].Split(',');
            _mapWidth = int.Parse(thirdLine[0]);
            _mapHeight = int.Parse(thirdLine[1]);

            var fourthLine = lines[3].Split(',');
            _map = new int[_mapWidth * _mapHeight];
            for (int y = 0; y < _mapWidth * _mapHeight; y++)
            {
                _map[y] = int.Parse(fourthLine[y]);
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            for (int y = 0; y < _mapWidth; y++)
            {
                for (int x = 0; x < _mapHeight; x++)
                {
                    int index = _map[y * _mapWidth + x] - 1;
                    if (index == -1)
                    {
                        continue;
                    }
                    spriteBatch.Draw(_tileSetTexture, new Vector2(x * _tileWidth, y * _tileHeight), _tiles[index], Color.White);
                }
            }
        }
    }
}
