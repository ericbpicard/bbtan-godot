#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

public partial class Cannon : Node2D
{
	[Export] public PackedScene BallScene { get; set; }  // Assign ball.tscn in Inspector
	[Export] public float ShootForce { get; set; } = 1800f;  // Tune speed
	[Export] public float BallSpread = 6f;
	[Export] public float ShootCooldown = 0.06f;
	[Export] public int BallsPerShot = 1;
	private Vector2 _dragStart;
	private bool isDragging = false;
	private Ball restingBall;  // Pre-shot ball at tip
	private List<Ball> activeBalls = new();
	private Timer cooldownTimer;
	private bool canShoot = true;
	private bool hasRestingBall = false;
	private Line2D _aimLine;
	private Line2D _aimLineBounce;
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
		_aimLine = GetNode<Line2D>("AimLine");
		_aimLine.Visible = false;
		_aimLineBounce = GetNode<Line2D>("AimLineBounce");
		_aimLineBounce.Visible = false;

		var spacingFactor = 3f;  // Adjust: >1.0 for gaps, 1.0 for touching


		var baseImg = Ball.CreateBallImage(_ballRadius);
		var diameter = baseImg.GetHeight();  // Assuming square (height = width)
		var period = (int)(diameter * spacingFactor);

		// Create wider image for spacing (ball + gap)
		var repeatImg = Image.CreateEmpty(period, diameter, false, Image.Format.Rgba8);

		// Blit the ball into the left part (centered if needed)
		repeatImg.BlitRect(baseImg, new Rect2I(0, 0, diameter, diameter), new Vector2I(0, 0));
		_aimLine.TextureRepeat = _aimLineBounce.TextureRepeat= CanvasItem.TextureRepeatEnum.Enabled;
		_aimLine.Texture = _aimLineBounce.Texture = ImageTexture.CreateFromImage(baseImg);
		_aimLine.TextureMode = _aimLineBounce.TextureMode = Line2D.LineTextureMode.Tile;
		_aimLine.Width = _aimLineBounce.Width =diameter;

		// NEW: Fade-out gradient**
		// var grad = new Gradient();
		// grad.AddPoint(0.0f, new Color(1, 1, 1, 1.0f));
		// grad.AddPoint(1.0f, new Color(1, 1, 1, 0.0f));
		//AimLine.Gradient = grad;

		// // NEW: Tapered width (thins at end for polish)**
		// var widthCurve = new Curve();
		// widthCurve.AddPoint(new Vector2(0.0f, 1.0f));
		// widthCurve.AddPoint(new Vector2(0.7f, 1.0f));
		// widthCurve.AddPoint(new Vector2(1.0f, 0.3f));
		// AimLine.WidthCurve = widthCurve;
		// AimLine.Width = 5.0f;  // Base width (tscn has 6.0; adjust taste)

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
						_dragStart = mouseButton.Position;  // Screen pos
						isDragging = true;
					}
					else if (isDragging)
					{
						var dragEnd = mouseButton.Position;
						GD.Print(dragEnd);
						var raw = (_dragStart - dragEnd).Normalized();  // Pull-back = shoot dir
						var aimVector = GetClampedAimVector(raw);
						canShoot = false;
						Shoot(aimVector);
						_aimLine.Visible = _aimLineBounce.Visible = false;

						isDragging = false;
						//Rotation = 0;  // Reset rotation
					}
				}
			}
			else if (@event is InputEventMouseMotion mouseMotion && isDragging)
			{
				var currentDragPos = mouseMotion.Position;
				var raw = (_dragStart - currentDragPos).Normalized();
				UpdateAimPreview(GetClampedAimVector(raw));
				_aimLine.Visible = _aimLineBounce.Visible = true;
			}
		}
		// For touch: Swap to InputEventScreenTouch (similar logic)
	}

	private void UpdateAimPreview(Vector2 dir)
	{
		if (restingBall == null)
		{
			_aimLine.ClearPoints();
			_aimLineBounce.ClearPoints();
			_aimLine.Visible = _aimLineBounce.Visible = false;
			return;
		}

		var spaceState = GetWorld2D().DirectSpaceState;
		Vector2 globalStart = restingBall.GlobalPosition;
		Vector2 globalFar = globalStart + dir * 1200f;

		// FIRST SEGMENT: Start → First Hit
		var query = PhysicsRayQueryParameters2D.Create(globalStart, globalFar);
		query.CollisionMask = 1;
		query.HitFromInside = false;
		var firstHit = spaceState.IntersectRay(query);

		_aimLine.ClearPoints();
		Vector2 localStart = ToLocal(globalStart);
		_aimLine.AddPoint(localStart);
		if (firstHit.Count > 0)
		{
			Vector2 hitPosG = firstHit["position"].AsVector2();
			Vector2 localHit = ToLocal(hitPosG);
			_aimLine.AddPoint(localHit);
		}
		else
		{
			Vector2 localFar = ToLocal(globalFar);
			_aimLine.AddPoint(localFar);
		}
		_aimLine.Visible = true;

		// SECOND SEGMENT: Bounce → End (separate line = no distortion)
		_aimLineBounce.ClearPoints();
		if (firstHit.Count > 0)
		{
			Vector2 hitPosG = firstHit["position"].AsVector2();
			Vector2 hitNormalG = firstHit["normal"].AsVector2();
			Vector2 reflectDir = dir.Bounce(hitNormalG);
			Vector2 secondStartG = hitPosG + hitNormalG * 0.02f;  // Epsilon offset (hides overlap)
			Vector2 secondFarG = secondStartG + reflectDir * 900f;

			var q2 = PhysicsRayQueryParameters2D.Create(secondStartG, secondFarG);
			q2.CollisionMask = 1;
			var secondHit = spaceState.IntersectRay(q2);

			Vector2 secondEndG = secondHit.Count > 0 ? secondHit["position"].AsVector2() : secondFarG;
			Vector2 localBounceStart = ToLocal(secondStartG);
			Vector2 localSecondEnd = ToLocal(secondEndG);

			_aimLineBounce.AddPoint(localBounceStart);
			_aimLineBounce.AddPoint(localSecondEnd);
			_aimLineBounce.Visible = true;
		}
		else
		{
			_aimLineBounce.Visible = false;
		}
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
		if (rawDirection.Y > -0.15f)   // not pointing up enough
		{
			// Force minimum upward angle
			return new Vector2(rawDirection.X, -0.15f).Normalized();
		}

		return rawDirection;   // already good
	}

	public void IncreaseBallsPerShot(int amount = 1)
	{
		BallsPerShot += amount;
		GD.Print($"Balls per shot increased to: {BallsPerShot}");
	}

	public void ScaleExistingBalls()
	{
		foreach (Ball x in activeBalls)
		{
			x.SetBallDamage(_main.GetDamageMult());
		}
	}
}