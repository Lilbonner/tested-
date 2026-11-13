using UnityEngine;

namespace ScenarioBuilder.Demo
{
    /// <summary>Простое управление капсулой: WASD / стрелки. Только для демонстрационной сцены.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] float speed = 5f;

        Rigidbody _body;
        Vector3 _input;

        void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.constraints = RigidbodyConstraints.FreezeRotation;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void Update()
        {
            _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            if (_input.sqrMagnitude > 1) _input.Normalize();
        }

        void FixedUpdate()
        {
            var velocity = _input * speed;
            velocity.y = _body.linearVelocity.y;
            _body.linearVelocity = velocity;
        }
    }
}
