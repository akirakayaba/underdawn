//Name:			Player.cs
//Description:	Controls for player movement, inputs,
//				attacking, and animations

using Godot;

public partial class Player : CharacterBody2D
{
	private AnimatedSprite2D animatedSprite;

	/*using string to store last input of user*/
	private string lastDirection = "down";

	/*speed key*/
	private float speed = 150f;

	private bool isAttacking = false;


	public override void _Ready()
{
	GD.Print("Player _Ready running, camera: ", GetViewport().GetCamera2D()?.GetPath());
	animatedSprite = GetNode<AnimatedSprite2D>("player_animation");
	animatedSprite.AnimationFinished += OnAnimationFinished;
	animatedSprite.Play("idle_" + lastDirection);

	GD.Print("PLAYER SCRIPT RUNNING");
	var cam = GetViewport().GetCamera2D();
	GD.Print("Camera: ", cam.GetPath(), "  Limits: ", cam.LimitLeft, ", ", cam.LimitTop, ", ", cam.LimitRight, ", ", cam.LimitBottom);
	//SetupCameraLimits();
}

// private void SetupCameraLimits()
// {
// 	var camera = GetNode<Camera2D>("Camera2D");
// 	var ground = GetTree().CurrentScene.GetNode<Sprite2D>("Ground");

// 	Vector2 size = ground.Texture.GetSize() * ground.Scale;
// 	Vector2 topLeft = ground.GlobalPosition;
// 	if (ground.Centered) topLeft -= size / 2f;

// 	camera.LimitLeft = (int)topLeft.X;
// 	camera.LimitTop = (int)topLeft.Y;
// 	camera.LimitRight = (int)(topLeft.X + size.X);
// 	camera.LimitBottom = (int)(topLeft.Y + size.Y);
	
// 	GD.Print($"Limits L{camera.LimitLeft} T{camera.LimitTop} R{camera.LimitRight} B{camera.LimitBottom}");
// }

	/*onAnimatonFinsished : to check if the attack is still going through*/
	private void OnAnimationFinished() => isAttacking = false;
	public override void _PhysicsProcess(double delta)
	{
		/*movement for player*/
		Vector2 direction = Input.GetVector(
		   "ui_left",
		   "ui_right",
		   "ui_up",
		   "ui_down"
	   );

		Velocity = direction * speed;
		MoveAndSlide();

		/*attack and attack animation for player*/
		if (!isAttacking)
		{
			if (Input.IsActionJustPressed("attack1"))
			{
				isAttacking = true;
				animatedSprite.Play("attack1_" + lastDirection);
			}
			if (Input.IsActionJustPressed("attack2"))
			{
				isAttacking = true;
				animatedSprite.Play("attack2_" + lastDirection);
			}
		}

		/*freeze movement temprorly until attack animation finshes*/
		if (isAttacking)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}
		/*movement animation for player*/
		if (direction != Vector2.Zero)
		{
			if (direction.X < 0)
				lastDirection = "left";
			else if (direction.X > 0)
				lastDirection = "right";
			else if (direction.Y < 0)
				lastDirection = "up";
			else if (direction.Y > 0)
				lastDirection = "down";

			animatedSprite.Play("run_" + lastDirection);
		}
		else
		{
			animatedSprite.Play("idle_" + lastDirection);
		}
	}
}
