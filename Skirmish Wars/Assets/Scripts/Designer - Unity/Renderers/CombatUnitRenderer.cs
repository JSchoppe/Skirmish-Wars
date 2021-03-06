﻿using System;
using UnityEngine;

namespace SkirmishWars.UnityRenderers
{
    // TODO clean up this struct.
    #region Exposed Structs
    [Serializable]
    public struct NumberSpriteSet
    {
        public Sprite zero;
        public Sprite one;
        public Sprite two;
        public Sprite three;
        public Sprite four;
        public Sprite five;
        public Sprite six;
        public Sprite seven;
        public Sprite eight;
        public Sprite nine;

        public Sprite this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return zero;
                    case 1: return one;
                    case 2: return two;
                    case 3: return three;
                    case 4: return four;
                    case 5: return five;
                    case 6: return six;
                    case 7: return seven;
                    case 8: return eight;
                    case 9: return nine;
                }
                return zero;
            }
        }
    }
    #endregion

    // TODO this class is getting fat, maybe abstract some parts.

    /// <summary>
    /// Implements a combat unit renderer that
    /// observes a combat unit instance.
    /// </summary>
    public sealed class CombatUnitRenderer : MonoBehaviour
    {
        #region Inspector Fields
        [Tooltip("The renderer used to render the unit.")]
        [SerializeField] private SpriteRenderer unitSprite = null;
        [Tooltip("The renderer used to render the hitpoints.")]
        [SerializeField] private SpriteRenderer hitpointsSprite = null;
        [Tooltip("The collection of sprites for the hitpoint numbering.")]
        [SerializeField] private NumberSpriteSet numberSpriteSet;
        [Tooltip("The sprite chain used to draw the unit path.")]
        [SerializeField] private SpriteChainRenderer movementChain = null;
        #endregion
        #region State Fields
        private Vector2[] worldPath;
        private CombatUnit drivingUnit;
        private TileIndicatorPool tileIndicatorPool;
        #endregion
        #region Initialization
        private void Awake()
        {
            // TODO figure out a better way to manage
            // the singleton here.
            tileIndicatorPool = FindObjectOfType<TileIndicatorPool>();
            if (tileIndicatorPool == null)
                throw new Exception("A tile indicator pool must be present in the scene.");
        }
        #endregion
        #region Observer Implementation
        /// <summary>
        /// The combat unit that this renderer is observing.
        /// </summary>
        public CombatUnit DrivingUnit
        {
            set
            {
                // Unbind from previous combat unit.
                if (drivingUnit != null)
                    RemoveListeners(drivingUnit);
                // Bind to new combat unit.
                drivingUnit = value;
                AddListeners(drivingUnit);
                // Initialize visual properties.
                OnHitPointsChanged(drivingUnit.hitPoints);
                OnTeamChanged(TeamsSingleton.FromID(drivingUnit.TeamID));
                // TODO intialization of more properties may
                // be needed to use object pooling.
            }
        }
        private void AddListeners(CombatUnit dispatcher)
        {
            dispatcher.PathChanged += OnPathChanged;
            dispatcher.TeamChanged += OnTeamChanged;
            dispatcher.Teleported += OnTeleported;
            dispatcher.MovementAnimating += OnMovementAnimating;
            dispatcher.HitPointsChanged += OnHitPointsChanged;
            dispatcher.PathShownChanged += OnPathShownChanged;
            dispatcher.FocusChanged += OnFocusChanged;
        }
        private void RemoveListeners(CombatUnit dispatcher)
        {
            dispatcher.PathChanged -= OnPathChanged;
            dispatcher.TeamChanged -= OnTeamChanged;
            dispatcher.Teleported -= OnTeleported;
            dispatcher.MovementAnimating -= OnMovementAnimating;
            dispatcher.HitPointsChanged -= OnHitPointsChanged;
            dispatcher.PathShownChanged -= OnPathShownChanged;
            dispatcher.FocusChanged -= OnFocusChanged;
        }
        #endregion
        #region Observer Listeners
        private void OnHitPointsChanged(float newHitPoints)
        {
            // If the new hit points are less than zero,
            // then this unit should be destroyed.
            if (newHitPoints < 0f)
            {
                RemoveListeners(drivingUnit);
                // TODO may want to implement object pool here.
                Destroy(gameObject);
                Destroy(movementChain.gameObject);
            }
            // Round up for the unit hitpoints.
            int hpNumber = Mathf.CeilToInt(newHitPoints * 10f);
            if (hpNumber > 9)
                hitpointsSprite.enabled = false;
            else
            {
                hitpointsSprite.enabled = true;
                // Update the hitpoint number displayed.
                hitpointsSprite.sprite = numberSpriteSet[hpNumber];
            }
        }
        private void OnTeamChanged(Team team)
        {
            // Set the visual properties of this unit.
            unitSprite.color = team.style.baseColor;
            unitSprite.flipX = team.style.flipX;
        }
        private void OnTeleported(Vector2Int newLocation)
        {
            // Move the transform to the new tile location.
            transform.position = drivingUnit.Grid.GridToWorld(newLocation);
        }
        private void OnPathChanged(Vector2Int[] newPath)
        {
            // Update the local world path and the
            // linked movement chain.
            worldPath = drivingUnit.Grid.GridToWorld(newPath);
            movementChain.Chain = worldPath;
        }
        private void OnMovementAnimating(float interpolant)
        {
            // Animate towards the next tile along the observed
            // units path.
            unitSprite.transform.position = 
                Vector2.Lerp(worldPath[0], worldPath[1], interpolant);
        }
        private void OnPathShownChanged(bool isShown)
        {
            // Hide all of the path in this renderer.
            movementChain.IsVisible = isShown;
        }
        private void OnFocusChanged(bool hasFocus)
        {
            // TODO this will cause collisions if multiple
            // units need to post to the indicator pool.
            if (hasFocus)
                tileIndicatorPool.SetIndicators(drivingUnit.PossibleDestinations, drivingUnit.Grid);
            else
                tileIndicatorPool.ClearIndicators();
        }
        #endregion
    }
}
