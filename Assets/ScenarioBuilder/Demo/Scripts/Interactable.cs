using UnityEngine;
using UnityEngine.Events;

namespace ScenarioBuilder.Demo
{
    /// <summary>
    /// Объект, с которым игрок взаимодействует клавишей (E) рядом с ним. Публикует обычный UnityEvent —
    /// в демо он подключён к UnityEventSource, так сценарий получает событие без связи кода.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] KeyCode key = KeyCode.E;
        [SerializeField] float radius = 2f;
        [SerializeField] string playerTag = "Player";
        [SerializeField] Renderer highlight;
        [SerializeField] Color highlightColor = new Color(1f, 0.85f, 0.3f);

        public UnityEvent onInteract = new UnityEvent();

        Transform _player;
        Color _baseColor;

        void Start()
        {
            var player = GameObject.FindWithTag(playerTag);
            if (player) _player = player.transform;
            if (highlight) _baseColor = highlight.material.color;
        }

        void Update()
        {
            if (_player == null) return;
            var inRange = Vector3.Distance(_player.position, transform.position) <= radius;
            if (highlight) highlight.material.color = inRange ? highlightColor : _baseColor;
            if (inRange && Input.GetKeyDown(key)) onInteract.Invoke();
        }
    }
}
