using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FamilyShooter;

public class Grid
{
    private class PointMass
    {
        // Const
        private const float BASE_DAMPING = 0.98f;

        // Parameters
        private float inverseMass;
        public float InverseMass => inverseMass;

        // State
        private Vector3 position;
        public Vector3 Position => position;

        private Vector3 velocity;
        public Vector3 Velocity => velocity;

        private Vector3 acceleration;
        private float damping;

        public PointMass(Vector3 pos, float invMass)
        {
            inverseMass = invMass;

            position = pos;
            velocity = Vector3.Zero;
            acceleration = Vector3.Zero;
            damping = BASE_DAMPING;
        }

        public void ApplyForce(Vector3 force)
        {
            acceleration += force * InverseMass;
        }

        public void IncreaseDamping(float factor)
        {
            damping *= factor;
        }

        public void Update()
        {
            // Tutorial uses acceleration per frame! We prefer elapsed time for stability, but it means
            // we must multiply all forces by 60!
            // Symplectic Euler integration: acceleration first, to make sure energy is preserved (spring
            // doesn't go farther each loop)
            // try reversed!
            velocity += acceleration * (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds;
            position += velocity * (float)GameRoot.GameTime.ElapsedGameTime.TotalSeconds;

            if (velocity.LengthSquared() < 0.001f * 0.001f)
            {
                velocity = Vector3.Zero;
            }

            // acceleration is summed from all contributions every frame,
            // so consume it now to avoid accumulation over time
            acceleration = Vector3.Zero;

            // apply damping factor
            velocity *= damping;

            // Tutorial: IncreaseDamping called every frame if needed, so reset to base damping each time
            // damping = BASE_DAMPING;

            // Turns out it's true only when bullet is around... why not, but I found that reverting to original damping
            // smoothly gives better results visually, as it allows looser grid
            float dampingDelta = damping - BASE_DAMPING;
            if (dampingDelta != 0f)
            {
                // Friction: go toward BASE_DAMPING, but not faster than 0.1 per frame
                // damping -= MathF.Sign(dampingDelta) * MathF.Min(0.1f, MathF.Abs(dampingDelta));
                damping = BASE_DAMPING;
            }
        }
    }

    // Tutorial uses struct, but I'd rather not copy every time...
    private class Spring
    {
        public PointMass End1;
        public PointMass End2;
        public float TargetLength;
        public float Stiffness;
        public float Damping;

        public Spring(PointMass end1, PointMass end2, float stiffness, float damping)
        {
            End1 = end1;
            End2 = end2;
            // slighlty less than normal to keep grid taut
            // try without!
            TargetLength = Vector3.Distance(end1.Position, end2.Position) * 0.95f;
            Stiffness = stiffness;
            Damping = damping;
        }

        public void Update()
        {
            // We work with 1 to 2 to compute force applied to End2, then oppose for End1
            Vector3 currentDelta = End2.Position - End1.Position;
            float currentLength = currentDelta.Length();
            float lengthDelta = currentLength - TargetLength;

            // these springs can only pull, not push
            // try without!
            if (lengthDelta <= 0)
            {
                return;
            }

            Vector3 currentDirection = currentDelta / currentLength;
            Vector3 dv = End2.Velocity - End1.Velocity;
            // Unlike tutorial we apply sign with stiffness now, as computed with 1 to 2
            Vector3 force2 = -Stiffness * lengthDelta * currentDirection - Damping * dv;

            End1.ApplyForce(-force2);
            End2.ApplyForce(force2);
        }
    }

    // Const
    private const int ANCHOR_POINT_PERIOD = 3;
    private const float INV_MASS = 1f;

    // Remember to multiply all values used for force/acceleration by 60
    private const float STIFFNESS = 60 * 0.28f;
    private const float SPRING_DAMPING = 60 * 0.06f;

    private const float STIFFNESS_BORDER = 60 * 0.1f;
    private const float SPRING_DAMPING_BORDER = 60 * 0.1f;

    // looser at center anchor points
    private const float STIFFNESS_CENTER_ANCHOR = 10 * 60 * 0.002f;
    private const float SPRING_DAMPING_CENTER_ANCHOR = 60 * 0.02f;

    private const float ANCHOR_LINE_THICKNESS = 3f;  // 3f in tutorial, but hard to see
    private const float DEFAULT_LINE_THICKNESS = 1f;  // 1f in tutorial, but hard to see

    // added for optimization
    private Rectangle size;
    private Vector2 spacing;

    private Spring[] springs;
    private PointMass[,] points;

    public Grid(Rectangle size, Vector2 spacing)
    {
        this.size = size;
        this.spacing = spacing;

        // unlike Tutorial, we don't divide size by spacing, but rather keep it as the grid size,
        // and multiply it by spacing to get world positions
        // I understood that Tutorial used Viewport.Bounds as Rect so it was just integer pixel coordinates
        // so instead, I now must divide the Bounds by the spacing
        points = new PointMass[size.Width + 1, size.Height + 1];

        // these fixed points will be used to anchor the grid to fixed positions on the screen
        // we'll only use some of them
        PointMass[,] fixedPoints = new PointMass[size.Width + 1, size.Height + 1];

        for (int i = 0; i < points.GetLength(0); i++)
        {
            for (int j = 0; j < points.GetLength(1); j++)
            {
                Vector3 pos = new Vector3((size.Left + i) * spacing.X, (size.Top + j) * spacing.Y, 0f);
                // edge points are immovable aka infinite mass aka inverse mass is 0
                // unlike Tutorial, we test perfect anchoring on the edges first
                float invMass = i == 0 || i == size.Width || j == 0 || j == size.Height ? 0 : INV_MASS;
                points[i, j] = new PointMass(pos, invMass);
                fixedPoints[i, j] = new PointMass(pos, invMass);
            }
        }

        // Horizontal springs: size.Width + 1 per row, size.Height + 1 rows
        // Vertical springs: size.Height + 1 per column, size.Width + 1 columns
        List<Spring> springsList = new List<Spring>(2 * (size.Width + 1) * (size.Height + 1));

        for (int i = 0; i < size.Width; i++)
        {
            for (int j = 0; j < size.Height; j++)
            {
                // TODO: indirect border anchors too
                if (i % ANCHOR_POINT_PERIOD == 0 && j % ANCHOR_POINT_PERIOD == 0)
                {
                    springsList.Add(new Spring(points[i, j], fixedPoints[i, j], STIFFNESS_CENTER_ANCHOR, SPRING_DAMPING_CENTER_ANCHOR));
                }

                // each point not on the bottom or right edge has a bottom and right neighbor
                springsList.Add(new Spring(points[i, j], points[i + 1, j], STIFFNESS, SPRING_DAMPING));
                springsList.Add(new Spring(points[i, j + 1], points[i, j], STIFFNESS, SPRING_DAMPING));
            }
        }

        springs = springsList.ToArray();
    }

    public void Update()
    {
        foreach (Spring spring in springs)
        {
            spring.Update();
        }

        foreach (PointMass pointMass in points)
        {
            pointMass.Update();
        }
    }

    private void GetRangeOfPointsInsideRadius(Vector3 center, float radius,
        out int left, out int right, out int top, out int bottom)
    {
        // Make sure to clamp to points dimensions
        left = Math.Max(0, (int)MathF.Ceiling((center.X - radius) / spacing.X) - size.Left);
        right = Math.Min(points.GetLength(0) - 1, (int)MathF.Ceiling((center.X + radius) / spacing.X) - size.Left);
        top = Math.Max(0, (int)MathF.Ceiling((center.Y - radius) / spacing.Y) - size.Top);
        bottom = Math.Min(points.GetLength(1) - 1, (int)MathF.Ceiling((center.Y + radius) / spacing.Y) - size.Top);
    }

    public void ApplyDirectedForce(Vector3 force, Vector3 position, float radius)
    {
        // Improvement over tutorial: only iterate on points likely to be in radius
        GetRangeOfPointsInsideRadius(position, radius, out int left, out int right, out int top, out int bottom);

        for (int i = left; i <= right; i++)
        {
            for (int j = top; j <= bottom; j++)
            {
                PointMass mass = points[i, j];

                if (Vector3.DistanceSquared(position, mass.Position) < radius * radius)
                    mass.ApplyForce(10 * force / (10 + Vector3.Distance(position, mass.Position)));
            }
        }
    }

    public void ApplyImplosiveForce(float force, Vector3 position, float radius, float dampingModifier = 0.6f)
    {
        // Improvement over tutorial: only iterate on points likely to be in radius
        GetRangeOfPointsInsideRadius(position, radius, out int left, out int right, out int top, out int bottom);

        for (int i = left; i <= right; i++)
        {
            for (int j = top; j <= bottom; j++)
            {
                PointMass mass = points[i, j];

                float dist2 = Vector3.DistanceSquared(position, mass.Position);
                if (dist2 < radius * radius)
                {
                    mass.ApplyForce(10 * force * (position - mass.Position) / (100 + dist2));
                    mass.IncreaseDamping(dampingModifier);
                }
            }
        }
    }

    public void ApplyExplosiveForce(float force, Vector3 position, float radius, float dampingModifier = 0.6f)
    {
        // Improvement over tutorial: only iterate on points likely to be in radius
        GetRangeOfPointsInsideRadius(position, radius, out int left, out int right, out int top, out int bottom);

        for (int i = left; i <= right; i++)
        {
            for (int j = top; j <= bottom; j++)
            {
                PointMass mass = points[i, j];

                float dist2 = Vector3.DistanceSquared(position, mass.Position);
                if (dist2 < radius * radius)
                {
                    mass.ApplyForce(100 * force * (mass.Position - position) / (10000 + dist2));
                    mass.IncreaseDamping(dampingModifier);
                }
            }
        }
    }

    public Vector2 ToVec2(Vector3 v)
    {
        // do a perspective projection
        float factor = (v.Z + 2000) / 2000;
        return (new Vector2(v.X, v.Y) - GameRoot.ScreenSize / 2f) * factor + GameRoot.ScreenSize / 2;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Color color = new Color(30, 30, 139, 85);   // dark blue (tutorial)
        Color color = new Color(47, 47, 255, 85);   // lighter blue, more visible
        // Color smoothedColor = new Color(0, 255, 0, 85);   // test to demonstrate smoothed points
        // Color nonSmoothedColor = new Color(255, 0, 0, 85);   // test to demonstrate non-smoothed points

        int width = points.GetLength(0);
        int height = points.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector2 start = ToVec2(points[i, j].Position);
                Vector2 rightNeighbor = default;
                Vector2 bottomNeighbor = default;

                if (i < width - 1)
                {
                    // Horizontal line to the right neighbor
                    rightNeighbor = ToVec2(points[i + 1, j].Position);
                    float thickness = j % ANCHOR_POINT_PERIOD == 0 ? ANCHOR_LINE_THICKNESS : DEFAULT_LINE_THICKNESS;

                    // Smooth things further with Catmull Room, using farther neighbors as extreme points
                    // Note that unlike Tutorial, we are iterating to the limit in every direction,
                    // so we need to clamp every time
                    Vector2 leftNeighbor = ToVec2(points[Math.Max(0, i - 1), j].Position);
                    Vector2 rightRightNeighbor = ToVec2(points[Math.Min(width - 1, i + 2), j].Position);
                    Vector2 smoothedMid = Vector2.CatmullRom(leftNeighbor, start, rightNeighbor, rightRightNeighbor, 0.5f);
                    if (Vector2.DistanceSquared(smoothedMid, (start + rightNeighbor) / 2f) > 1f)
                    {
                        // More than 1px between smoothed mid and linear mid point, worth using it
                        spriteBatch.DrawLine(start, smoothedMid, color, thickness);
                        spriteBatch.DrawLine(smoothedMid, rightNeighbor, color, thickness);
                    }
                    else
                    {
                        // Linear mid point is good enough
                        spriteBatch.DrawLine(start, rightNeighbor, color, thickness);
                    }
                }

                if (j < height - 1)
                {
                    // Vertical line to the bottom neighbor
                    bottomNeighbor = ToVec2(points[i, j + 1].Position);
                    float thickness = i % ANCHOR_POINT_PERIOD == 0 ? ANCHOR_LINE_THICKNESS : DEFAULT_LINE_THICKNESS;

                    // Same smoothing
                    Vector2 topNeighbor = ToVec2(points[i, Math.Max(0, j - 1)].Position);
                    Vector2 bottomBottomNeighbor = ToVec2(points[i, Math.Min(height - 1, j + 2)].Position);
                    Vector2 smoothedMid = Vector2.CatmullRom(topNeighbor, start, bottomNeighbor, bottomBottomNeighbor, 0.5f);
                    if (Vector2.DistanceSquared(smoothedMid, (start + bottomNeighbor) / 2f) > 1f)
                    {
                        // More than 1px between smoothed mid and linear mid point, worth using it
                        spriteBatch.DrawLine(start, smoothedMid, color, thickness);
                        spriteBatch.DrawLine(smoothedMid, bottomNeighbor, color, thickness);
                    }
                    else
                    {
                        // Linear mid point is good enough
                        spriteBatch.DrawLine(start, bottomNeighbor, color, thickness);
                    }
                }

                // Interpolate intermediate lines at low cost
                if (i < width - 1 && j < height - 1)
                {
                    // In theory we should interpolate between the Catmull-Rom smoothed points too, but they proved very close
                    // to the linear mid points, so it will be enough to draw straight intermediate lines
                    Vector2 bottomRightNeighbor = ToVec2(points[i + 1, j + 1].Position);
                    // // Mid horizontal line
                    spriteBatch.DrawLine(0.5f * (start + bottomNeighbor), 0.5f * (rightNeighbor + bottomRightNeighbor), color, DEFAULT_LINE_THICKNESS);
                    // // Mid vertical line
                    spriteBatch.DrawLine(0.5f * (start + rightNeighbor), 0.5f * (bottomNeighbor + bottomRightNeighbor), color, DEFAULT_LINE_THICKNESS);
                }
            }
        }
    }
}
