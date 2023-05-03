using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CreatureController : MonoBehaviour
{
    protected float _speed = 5.0f;
    public Vector3Int CellPos { get; set; } = Vector3Int.zero;
    protected Animator _animator;
    protected SpriteRenderer _sprite;


    protected CreatureState _state = CreatureState.Idle;
    public virtual CreatureState State
    {
        get { return _state; }
        set
        {
            if(_state == value)
            return;

            _state = value;
            UpdateAnimation();
        }
    }

    protected MoveDir _lastDir = MoveDir.Down;
    protected MoveDir _dir = MoveDir.Down;
    public MoveDir Dir
    {
        get { return _dir; }
        set
        {
            if (_dir == value)
                return;


            _dir = value;
            if (value != MoveDir.None)
                _lastDir = value;

            UpdateAnimation();
        }
    }

    public Vector3Int GetFrontCellPos()
    {
        Vector3Int cellPos = CellPos;

        switch(_lastDir)
        {
            case MoveDir.Up:
                cellPos += Vector3Int.up;
                break;
            case MoveDir.Down:
                cellPos += Vector3Int.down;
                break;
            case MoveDir.Left:
                cellPos += Vector3Int.left;
                break;
            case MoveDir.Right:
                cellPos += Vector3Int.right;
                break;
        }
        return cellPos;
    }

    protected virtual void UpdateAnimation()
    {
        if(_state == CreatureState.Idle)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play("Idle_Back");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("Idle_Front");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("Idle_Right");
                    _sprite.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("Idle_Right");
                    _sprite.flipX = false;
                    break;
            }
        }
        else if(_state == CreatureState.Moving)
        {
            switch (_dir)
            {
                case MoveDir.Up:
                    _animator.Play("Walk_Back");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("Walk_Front");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("Walk_Right");
                    _sprite.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("Walk_Right");
                    _sprite.flipX = false;
                    break;

            }
        }
        else if (_state == CreatureState.Skill)
        {
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play("Attack_Back");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("Attack_Front");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("Attack_Right");
                    _sprite.flipX = true;
                    break;
                case MoveDir.Right:
                    _animator.Play("Attack_Right");
                    _sprite.flipX = false;
                    break;

            }
        }
        else
        {

        }
    }
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateController();

    }

    protected virtual void Init()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        Vector3 pos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
        transform.position = pos;
    }

    protected virtual void UpdateController()
    {
        switch(State)
        {
            case CreatureState.Idle:
                UpdateIdle();
                break;
            case CreatureState.Moving:
                UpdateMoving();
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
        }

        
    }

    // 捞悼 贸府
    protected virtual void UpdateMoving()
    {
        Vector3 destpos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
        Vector3 moveDir = destpos - transform.position;

        float dist = moveDir.magnitude;
        if (dist < _speed * Time.deltaTime)
        {
            transform.position = destpos;
            //局聪皋捞记 流立 牧飘费
            MoveToNextPos();
        }
        else
        {
            transform.position += moveDir.normalized * _speed * Time.deltaTime;
            State = CreatureState.Moving;
        }
    }

    protected virtual void UpdateIdle()
    {

    }

    protected virtual void MoveToNextPos()
    {
        if(_dir == MoveDir.None)
        {
            State = CreatureState.Idle;
            return;
        }

        Vector3Int destPos = CellPos;

        switch (_dir)
        {
            case MoveDir.Up:
                destPos += Vector3Int.up;
                break;
            case MoveDir.Down:
                destPos += Vector3Int.down;
                break;
            case MoveDir.Left:
                destPos += Vector3Int.left;
                break;
            case MoveDir.Right:
                destPos += Vector3Int.right;
                break;
        }

        if (Managers.Map.CanGo(destPos))
        {
            if (Managers.Object.Find(destPos) == null)
            {
                CellPos = destPos;

            }
        }
    }

    protected virtual void UpdateSkill()
    {

    }

    protected virtual void UpdateDead()
    {

    }

    public virtual void OnDamaged()
    {

    }
}
