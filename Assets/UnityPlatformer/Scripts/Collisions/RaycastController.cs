/**!
The MIT License (MIT)

Copyright (c) 2015 Sebastian
Original file: https://github.com/SebLague/2DPlatformer-Tutorial/blob/master/Episode%2011/RaycastController.cs

Modifications (c) 2016 Luis Lafuente

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
**/

﻿using System;
﻿using UnityEngine;
using System.Collections;

namespace UnityPlatformer {
  [RequireComponent (typeof (BoxCollider2D))]
  public class RaycastController : MonoBehaviour {

    public LayerMask collisionMask;

    /// <summary>
    /// How far from then env the Character must be.
    /// NOTE: must be less than skinWidth, to allow continuous ground contact
    /// </summary>
    public float minDistanceToEnv = 0.1f;
    /// <summary>
    /// Defines how far in from the edges of the collider rays are we going to cast from.
    /// NOTE: This value must be greater than minDistanceToEnv
    /// </summary>
    public float skinWidth = 0.2f;
    /// <summary>
    /// How many rays to check horizontal collisions
    /// </summary>
    public int horizontalRayCount = 4;
    /// <summary>
    /// How many rays to check vertical collisions
    /// </summary>
    public int verticalRayCount = 4;

    internal float horizontalRaySpacing;
    internal float verticalRaySpacing;

    internal BoxCollider2D box;

    internal RaycastOrigins raycastOrigins;
    internal RaycastHit2D[] horizontalRays;
    internal RaycastHit2D[] verticalRays;

    internal Bounds bounds;
    internal float skinWidthMagnitude;

    public virtual void OnEnable() {
      box = GetComponent<BoxCollider2D> ();
      CalculateRaySpacing ();
      UpdateInnerBounds();

      skinWidthMagnitude = Mathf.Sqrt(skinWidth + skinWidth);

      if (horizontalRays == null) {
        horizontalRays = new RaycastHit2D[horizontalRayCount];
      }

      if (verticalRays == null) {
        verticalRays = new RaycastHit2D[verticalRayCount];
      }
    }

    public void UpdateInnerBounds() {
      bounds = box.bounds;
      // * 2 so it's shrink skinWidth by each side
      bounds.Expand (skinWidth * -2);
    }

    public void UpdateRaycastOrigins() {
      UpdateInnerBounds();
      CalculateRaySpacing();

      // cache
      Vector3 min = bounds.min;
      Vector3 max = bounds.max;
      float half_width = bounds.size.x * 0.5f;

      raycastOrigins.bottomLeft = new Vector2 (min.x, min.y);
      raycastOrigins.bottomCenter = new Vector2 (min.x + half_width, min.y);
      raycastOrigins.bottomRight = new Vector2 (max.x, min.y);
      raycastOrigins.topLeft = new Vector2 (min.x, max.y);
      raycastOrigins.topCenter = new Vector2 (min.x + half_width, max.y);
      raycastOrigins.topRight = new Vector2 (max.x, max.y);
    }

    /// <summary>
    /// Recalculate distance between rays (horizontalRaySpacing & verticalRaySpacing)
    /// </summary>
    public void CalculateRaySpacing() {
      horizontalRayCount = Mathf.Clamp (horizontalRayCount, 2, int.MaxValue);
      verticalRayCount = Mathf.Clamp (verticalRayCount, 2, int.MaxValue);

      horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
      verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public RaycastHit2D Raycast(Vector2 origin, Vector2 direction, float rayLength, int mask, Color? color = null) {
      Debug.DrawRay(origin, direction * rayLength, color ?? Color.red);

      return Physics2D.Raycast(origin, direction, rayLength, mask);
    }

    [Serializable]
    public struct RaycastOrigins {
      public Vector2 topLeft;
      public Vector2 topCenter;
      public Vector2 topRight;
      public Vector2 bottomLeft;
      public Vector2 bottomCenter;
      public Vector2 bottomRight;
    }

    public RaycastHit2D DoVerticalRay(float directionY, int i, float rayLength, ref Vector3 velocity, Color? c = null) {
        Vector2 rayOrigin = (directionY == -1) ?
          raycastOrigins.bottomLeft :
          raycastOrigins.topLeft;

        rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
        RaycastHit2D hit = Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask, c ?? Color.red);

        return hit;
    }

    public RaycastHit2D DoFeetRay(float rayLength, LayerMask mask) {
      return Raycast(raycastOrigins.bottomCenter, Vector2.down, rayLength, mask, Color.blue);
    }

    public delegate void RayItr(ref RaycastHit2D hit, ref Vector3 velocity, int dir, int idx);

    public void ForeachRightRay(float rayLength, ref Vector3 velocity, RayItr itr) {
      if (velocity.x > 0) {
        rayLength += velocity.x;
      }

      Vector3 origin = raycastOrigins.bottomRight;
      origin.y += velocity.y;

      for (int i = 0; i < horizontalRayCount; i ++) {

        horizontalRays[i] = Raycast(origin, Vector2.right, rayLength, collisionMask, new Color(1, 0, 0, 0.5f));
        origin.y += horizontalRaySpacing;

        itr(ref horizontalRays[i], ref velocity, 1, i);
      }
    }

    public void ForeachLeftRay(float rayLength, ref Vector3 velocity, RayItr itr) {
      if (velocity.x < 0) {
        rayLength -= velocity.x;
      }

      Vector3 origin = raycastOrigins.bottomLeft;
      origin.y += velocity.y;

      for (int i = 0; i < horizontalRayCount; i ++) {

        horizontalRays[i] = Raycast(origin, Vector2.left, rayLength, collisionMask, new Color(1, 0, 0, 0.5f));
        origin.y += horizontalRaySpacing;

        itr(ref horizontalRays[i], ref velocity, -1, i);
      }
    }

    public void ForeachHeadRay(float rayLength, ref Vector3 velocity, RayItr itr) {
      if (velocity.y > 0) {
        rayLength += velocity.y;
      }

      Vector3 origin = raycastOrigins.topLeft;
      origin.x += velocity.x;

      for (int i = 0; i < verticalRayCount; i ++) {

        verticalRays[i] = Raycast(origin, Vector2.up, rayLength, collisionMask, new Color(1, 0, 0, 0.5f));
        origin.x += verticalRaySpacing;

        itr(ref verticalRays[i], ref velocity, 1, i);
      }
    }

    public void ForeachFeetRay(float rayLength, ref Vector3 velocity, RayItr itr) {
      Vector3 origin = raycastOrigins.bottomLeft;
      origin.x += velocity.x;
      float length;

      for (int i = 0; i < verticalRayCount; i ++) {
        length = velocity.y < 0 ? rayLength - velocity.y : rayLength;

        verticalRays[i] = Raycast(origin, Vector2.down, length, collisionMask, new Color(1, 0, 0, 0.5f));
        origin.x += verticalRaySpacing;

        itr(ref verticalRays[i], ref velocity, -1, i);
      }
    }

    public RaycastHit2D LeftFeetRay(float rayLength, Vector3 velocity) {
      if (velocity.y < 0) {
        rayLength -= velocity.y;
      }

      Vector3 origin = raycastOrigins.bottomLeft;
      origin.x += velocity.x;

      return Raycast(origin, Vector2.down, rayLength, collisionMask, Color.yellow);
    }

    public RaycastHit2D RightFeetRay(float rayLength, Vector3 velocity) {
      if (velocity.y < 0) {
        rayLength -= velocity.y;
      }

      Vector3 origin = raycastOrigins.bottomLeft;
      origin.x += velocity.x + verticalRaySpacing * verticalRayCount;

      return Raycast(origin, Vector2.down, rayLength, collisionMask, Color.yellow);
    }
  }
}
