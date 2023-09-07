using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JetpackGame.Player;

partial class JetpackController : EntityComponent<JetpackPlayer>
{
	public int StepSize => 24;
	public int GroundAngle => 45;
	public int JumpSpeed => 20;

	float Roll => 0;
	HashSet<string> ControllerEvents = new( StringComparer.OrdinalIgnoreCase );
	bool Grounded => Entity.GroundEntity.IsValid();

	public void Simulate( IClient cl )
	{
		if ( Entity.IsDead ) return;

		//Log.Trace( $"Velocity {Entity.Velocity.Length}" );

		ControllerEvents.Clear();

		// build movement from the input values
		Vector3 movement = Entity.InputDirection
			// x=forward,back | y=strafe | z=space?
			//.Clamp( new Vector3( 0, 0, -1 ), new Vector3( 1, 0, 1 ) )
			//.WithY( 0 )
			.WithZ( 0 )
			.Normal;

		//Roll += 

		Angles angles = Entity.Velocity.EulerAngles.WithRoll( Roll );
		//Angles angles = Entity.ViewAngles.WithPitch( 0f );

		if ( Entity.Stance == JetpackStance.Hover )
		{
			angles = Entity.ViewAngles.WithPitch( -75 );
		}

		// Get wish movement vector
		var moveVector = (Rotation.From( angles ) * movement * 320f);
		// Set ground ent
		var groundEntity = CheckForGround();

		//if ( Input.Down( "jump" ) )
		//{
		//	//if ( Grounded )
		//	//{
		//	//}
		//	AddEvent( "jump" );

		//	Entity.Velocity += Vector3.Up * JumpSpeed;
		//}

		if ( groundEntity.IsValid() )
		{
			// We're grounded, negate gravity and slow down!

			if ( !Grounded )
			{
				Entity.Velocity = Entity.Velocity.WithZ( 0 );
				AddEvent( "grounded" );
			}

			Entity.Stance = JetpackStance.Hover;
			Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, 200.0f * (Input.Down( "run" ) ? 2.5f : 1f), 7.5f );
			Entity.Velocity = ApplyFriction( Entity.Velocity, 2.0f );
		}
		else
		{
			// We're in the air, maintain velocity!

			Entity.Stance = JetpackStance.Forward;
			Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, 100, 20f );
			Entity.Velocity += Vector3.Down * Math.Abs( Game.PhysicsWorld.Gravity.Length ) * Time.Delta;
		}


		//if ( Input.Down( "run" ) )
		//{
		//	Entity.Velocity += Entity.Velocity.Normal * 1.5f;
		//}

		MoveHelper helper = new( Entity.Position, Entity.Velocity );
		helper.Trace = helper.Trace.Size( 16 ).Ignore( Entity );
		if ( helper.TryMove( Time.Delta ) > 0 )
		{
			Entity.Position = helper.Position;
			Entity.Velocity = helper.Velocity;
		}

		Entity.Rotation = angles.ToRotation();

		Entity.GroundEntity = groundEntity;

		//var mh = new MoveHelper( Entity.Position, Entity.Velocity );
		//mh.Trace = mh.Trace.Size( Entity.Hull ).Ignore( Entity );

		//if ( mh.TryMoveWithStep( Time.Delta, StepSize ) > 0 )
		//{
		//	if ( Grounded )
		//	{
		//		mh.Position = StayOnGround( mh.Position );
		//	}
		//}
	}

	Entity CheckForGround()
	{
		if ( Entity.Velocity.z > 100f )
			return null;

		var trace = Entity.TraceCapsule( Entity.Position, Entity.Position + Vector3.Down);

		//Log.Info( trace.Entity );
		if ( !trace.Hit )
			return null;

		if ( trace.Normal.Angle( Vector3.Up ) > GroundAngle )
			return null;

		return trace.Entity;
	}

	Vector3 ApplyFriction( Vector3 input, float frictionAmount )
	{
		float StopSpeed = 100.0f;

		var speed = input.Length;
		if ( speed < 0.1f ) return input;

		// Bleed off some speed, but if we have less than the bleed
		// threshold, bleed the threshold amount.
		float control = (speed < StopSpeed) ? StopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;
		if ( newspeed == speed ) return input;

		newspeed /= speed;
		input *= newspeed;

		return input;
	}

	Vector3 Accelerate( Vector3 input, Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = input.Dot( wishdir );
		var addspeed = wishspeed - currentspeed;

		if ( addspeed <= 0 )
			return input;

		var accelspeed = acceleration * Time.Delta * wishspeed;

		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		input += wishdir * accelspeed;

		return input;
	}

	Vector3 StayOnGround( Vector3 position )
	{
		var start = position + Vector3.Up * 2;
		var end = position + Vector3.Down * StepSize;

		// See how far up we can go without getting stuck
		var trace = Entity.TraceCapsule( position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = Entity.TraceCapsule( start, end );

		if ( trace.Fraction <= 0 ) return position;
		if ( trace.Fraction >= 1 ) return position;
		if ( trace.StartedSolid ) return position;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return position;

		return trace.EndPosition;
	}

	public bool HasEvent( string eventName )
	{
		return ControllerEvents.Contains( eventName );
	}

	void AddEvent( string eventName )
	{
		if ( HasEvent( eventName ) )
			return;

		ControllerEvents.Add( eventName );
	}
}


partial class JetpackPlayer : AnimatedEntity
{
	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;
		ViewAngles = (ViewAngles += Input.AnalogLook).Normal;
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		//Rotation = ViewAngles.ToRotation();


		var angles = ViewAngles.ToRotation();
		Vector3 adjustment = 
			(angles.Backward * 180) +
			(angles.Up * 45) +
			(angles.Right * 40);

		var backAdjustment = angles.Backward * Math.Clamp( Velocity.Length / 1000, 0f, 120f );
		var chasePos = Position + adjustment + backAdjustment;
		var speedAdjustment = Math.Clamp( Velocity.Length / 1000, 0.5f, 1f );
		var position = Position.LerpTo( chasePos, new Vector3( speedAdjustment, speedAdjustment, speedAdjustment ) );

		// Position camera back, up, and right (shoulder cam)
		Camera.Position = position;
		Camera.Rotation = angles;

		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		// Set the first person viewer to this, so it won't render our model
		//Camera.FirstPersonViewer = this;
		//Camera.FirstPersonViewer = null;
	}
}
