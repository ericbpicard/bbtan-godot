#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public partial class Cannon : Node2D
{
	[Export] public PackedScene BallScene { get; set; }  // Assign ball.tscn in Inspector
	[Export] public Line2D AimLine { get; set; }
	[Export] public float ShootForce { get; set; } = 1800f;  // Tune speed
	[Export] public float BallSpread = 6f;
	[Export] public float ShootCooldown = 0.06f;
	[Export] public int BallsPerShot = 1;
	private Vector2 dragStart;
	private bool isDragging = false;
	private Ball restingBall;  // Pre-shot ball at tip
	private List<Ball> activeBalls = new();
	private Timer cooldownTimer;
	private bool canShoot = true;
	private bool hasRestingBall = false;
	private Main? _main;
	public float _ballRadius = 12f;

	//start passing stuff 
	// instead of countless class properties?


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_main = GetParent<Main>();
		var screenSize = GetViewportRect().Size;
		Position = new Vector2(screenSize.X / 2f, screenSize.Y - 50f);
		SpawnRestingBall(Vector2.Zero);
		canShoot = true;
		AimLine = GetNode<Line2D>("AimLine");
		AimLine.Visible = false;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		// Touch/Mouse drag: Start near cannon → Pull back → Release to shoot
		if (canShoot)
		{
			if (@event is InputEventMouseButton mouseButton)
			{
				if (mouseButton.ButtonIndex == MouseButton.Left)
				{
					if (mouseButton.Pressed)
					{
						dragStart = mouseButton.Position;  // Screen pos
						isDragging = true;
					}
					else if (isDragging)
					{
						var dragEnd = mouseButton.Position;
						GD.Print(dragEnd);
						var raw = (dragStart - dragEnd).Normalized();  // Pull-back = shoot dir
						var aimVector = GetClampedAimVector(raw);
						canShoot = false;
						Shoot(aimVector);
						AimLine.Visible = false;
						isDragging = false;
						//Rotation = 0;  // Reset rotation
					}
				}
			}
			else if (@event is InputEventMouseMotion mouseMotion && isDragging)
			{
				var currentDragPos = mouseMotion.Position;
				var raw = (dragStart - currentDragPos).Normalized();
				var lineVector = GetClampedAimVector(raw);
				AimLine.Visible = true;
				GD.Print(currentDragPos);
				AimLine.ClearPoints();
				AimLine.AddPoint(restingBall.Position);           // start at cannon
				AimLine.AddPoint(restingBall.Position + lineVector * 2500);          // long line in shoot direction
			}
		}
		// For touch: Swap to InputEventScreenTouch (similar logic)
	}

	private void UpdateAimPreview()
	{

		///implement a bouncing dotted aimline
		if (restingBall == null) return;
	}

	public void SpawnRestingBall(Vector2 position)
	{
		restingBall = BallScene.Instantiate<Ball>();
		restingBall.Init(_ballRadius);
		restingBall.DisableBallCollision();
		restingBall.Position = position;

		restingBall.LinearVelocity = Vector2.Zero;  // Stationary

		CallDeferred(MethodName.AddChild, restingBall);
		hasRestingBall = true;
	}

	private async Task Shoot(Vector2 aimVector)
	{
		if (restingBall == null) return;
		//	GD.PrintErr(restingBall.LinearDampMode.ToString());
		for (int i = 0; i < BallsPerShot; i++)
		{
			FireSingleBall(aimVector);
			await ToSignal(GetTree().CreateTimer(ShootCooldown), SceneTreeTimer.SignalName.Timeout);

		}
		restingBall?.QueueFree();  // Clean up old one if exists
		restingBall = null;
		hasRestingBall = false;
	}

	private void FireSingleBall(Vector2 aimVector)
	{
		var currentBall = BallScene.Instantiate<Ball>();
		currentBall.Init(_ballRadius);
		currentBall.CollisionLayer = 2;
		currentBall.CollisionMask = 1;
		currentBall.GlobalPosition = restingBall.GlobalPosition;
		currentBall.LinearVelocity = Vector2.Zero;
		currentBall.SetBallDamage(_main.GetDamageMult());
		GetTree().CurrentScene.CallDeferred(MethodName.AddChild, currentBall);
		activeBalls.Add(currentBall);
		currentBall.ApplyCentralImpulse(aimVector * ShootForce);
	}

	public void OnBallLanded(Node body)
	{
		GD.Print(body.GetType());
		if (body is Ball ball)
		{
			Vector2 landedPosition = ball.GlobalPosition;
			landedPosition.Y = GetViewportRect().Size.Y - 50f;
			ball.GlobalPosition = landedPosition;
			ball.LinearVelocity = new Vector2(0, 0);

			if (!hasRestingBall)
			{
				Position = landedPosition;
				SpawnRestingBall(Vector2.Zero);
				ball.QueueFree();
				activeBalls.Remove(ball);
				CheckAllLanded();
			}
			else
			{
				var tween = CreateTween();
				tween.TweenProperty(ball, "global_position", GlobalPosition, 0.55f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
				tween.TweenCallback(Callable.From(() =>
				{
					ball.QueueFree();
					activeBalls.Remove(ball);
					CheckAllLanded();
				}));
			}
		}
	}
	private void CheckAllLanded()
	{
		if (activeBalls.Count == 0)
		{
			var main = GetTree().CurrentScene as Main;
			main?.CallDeferred(Main.MethodName.AddNewRow);
			canShoot = true;
		}
	}


	private Vector2 GetClampedAimVector(Vector2 rawDirection)
	{
		// rawDirection is already normalized
		if (rawDirection.Y > -0.25f)   // not pointing up enough
		{
			// Force minimum upward angle
			return new Vector2(rawDirection.X, -0.25f).Normalized();
		}

		return rawDirection;   // already good
	}

	public void IncreaseBallsPerShot(int amount = 1)
	{
		BallsPerShot += amount;
		GD.Print($"Balls per shot increased to: {BallsPerShot}");
	}
}