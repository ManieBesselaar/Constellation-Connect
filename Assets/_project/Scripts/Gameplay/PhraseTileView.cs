namespace Constellation.Gameplay
{
    using System;
    using System.Collections.Generic;
    using Constellation.Audio;
    using Constellation.Data;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// Draggable phrase tile. Lifts above the pointer while held and reports its drop
    /// target on release.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PhraseTileView : MonoBehaviour, IDraggableTile, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Vertical offset applied so a finger does not cover the tile.</summary>
        private const float TouchYOffset = 85f;

        [Tooltip("Label showing the chunk text.")]
        [SerializeField] private TextMeshProUGUI _label;

        [Tooltip("Background image tinted gold once placed.")]
        [SerializeField] private Image _background;

        [Tooltip("Colour used while the tile is held.")]
        [SerializeField] private Color _heldColor = new Color(1f, 0.84f, 0.44f, 1f);

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private Vector2 _origin;
        private Transform _originParent;
        private Color _restColor = Color.white;

        /// <summary>Raised when the tile is released, with the target under the pointer.</summary>
        public event Action<PhraseTileView, IDropTarget> Dropped;

        /// <inheritdoc />
        public PhraseChunk Chunk { get; private set; }

        /// <inheritdoc />
        public bool IsDragging { get; private set; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvas = GetComponentInParent<Canvas>();

            if (_background != null) _restColor = _background.color;
        }

        /// <summary>
        /// Binds the tile to a chunk.
        /// </summary>
        /// <param name="chunk">The chunk this tile carries.</param>
        public void Bind(PhraseChunk chunk)
        {
            Chunk = chunk;
            if (_label != null) _label.text = chunk != null ? chunk.text : string.Empty;
        }

        /// <summary>
        /// Sets the label text, used to render the localized chunk instead of the raw fallback.
        /// </summary>
        /// <param name="text">Text to display.</param>
        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Chunk == null) return;

            AudioManager.PlayTilePickup();

            IsDragging = true;
            _origin = _rect.anchoredPosition;
            _originParent = transform.parent;

            if (_canvas != null)
            {
                transform.SetParent(_canvas.transform, true);

                // GridLayoutGroup pins a child's anchors to the tray's top-left corner (0,1), so
                // anchoredPosition would be measured from that corner once we are on the canvas.
                // Centre the anchors here so OnDrag's canvas-space local point maps 1:1.
                _rect.anchorMin = new Vector2(0.5f, 0.5f);
                _rect.anchorMax = new Vector2(0.5f, 0.5f);
            }

            transform.SetAsLastSibling();
            _canvasGroup.blocksRaycasts = false;

            if (_background != null) _background.color = _heldColor;
        }

        /// <inheritdoc />
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            if (_canvas == null)
            {
                _rect.position = eventData.position;
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 local);

            _rect.anchoredPosition = local + new Vector2(0f, TouchYOffset * 0.5f);
        }

        /// <inheritdoc />
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            IsDragging = false;
            _canvasGroup.blocksRaycasts = true;
            if (_background != null) _background.color = _restColor;

            IDropTarget target = FindTargetUnderPointer(eventData);
            Dropped?.Invoke(this, target);

            if (target == null || !target.IsFilled) ReturnToOrigin();
        }

        /// <inheritdoc />
        public void ReturnToOrigin()
        {
            if (_originParent != null) transform.SetParent(_originParent, false);
            _rect.anchoredPosition = _origin;
        }

        private IDropTarget FindTargetUnderPointer(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject == null) continue;

                var target = result.gameObject.GetComponentInParent<IDropTarget>();
                if (target != null && target != (IDropTarget)this) return target;
            }

            return null;
        }
    }
}
