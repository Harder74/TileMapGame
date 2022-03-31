using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;


namespace TileMapGame.Collisions
{
    public struct BoundingCircle
    {
        public Vector2 Center;
        public float Radius;

        public BoundingCircle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// tests for collision between this and another bounding circle
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CollidesWith(BoundingCircle other)
        {
            return CollisionHelper.Collides(this, other);
        }

        public bool CollidesWith(BoundingRectangle other)
        {
            return CollisionHelper.Collides(this, other);
        }
    }
}
