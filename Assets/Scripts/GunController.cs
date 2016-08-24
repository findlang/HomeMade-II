﻿using UnityEngine;
using System.Collections;

public class GunController : MonoBehaviour {

	CameraController cameraController;
	public GameObject bullet;
	public GameObject muzzleFlash;
	public GameObject owner;
	
	//stuff that can change
	public int maxAmmo = 3;
	float maxInaccuracy = 0.05f;
	float recoilForce = 0.3f;
	float shootForce = 20f;

	private bool upButton, leftButton, downButton, rightButton;

	enum EDir {
		N, NE, E, SE, S, SW, W, NW
	};

	public int ammo;
	bool isHeld = true;
	EDir shootDir;
	Vector3 shootVector;

	public virtual void Start () {
		cameraController = Camera.main.GetComponent<CameraController>();

		muzzleFlash = Instantiate(muzzleFlash, transform.position + new Vector3 (0.26f,0.0333f,0), Quaternion.identity) as GameObject;
		muzzleFlash.transform.parent = transform;
		muzzleFlash.SetActive(false);

		Reload();

		transform.position = owner.transform.position; // HACK ?
		//color
		GetComponent<SpriteRenderer>().color = owner.GetComponent<PlayerController>().m_playerNumber == 1 ? Color.red : Color.blue;
	}
	
	public virtual void Update () {
		if (Input.GetButtonDown(owner.GetComponent<PlayerController>().m_fireButton) && owner.GetComponent<PlayerController>().m_hasControl) {
			Shoot ();
		}

		if (Input.GetKeyDown(KeyCode.U)) isHeld = !isHeld;

		if (ammo != maxAmmo && cameraController.IsOnScreen(owner.transform.position)) {
			ammo = maxAmmo;
		}

		SetShootDir();
		SetRotation();
	}

	public virtual void Shoot() {
		if (ammo > 0) {
			//make a bullet
			GameObject newBullet = Instantiate(bullet, owner.transform.position, Quaternion.identity) as GameObject;
			//fire in that direction
			newBullet.GetComponent<BulletController>().velocity = shootVector * shootForce;

			//lil bit of spin NO MORE
			//newBullet.GetComponent<BulletController>().direction += new Vector3(Random.Range(-maxInaccuracy, maxInaccuracy),Random.Range(-maxInaccuracy, maxInaccuracy),0);

			//autoaim???
			Vector3 rPos = Vector3.Normalize(GameObject.FindWithTag("Player2").transform.position - transform.position);
			newBullet.GetComponent<BulletController>().velocity += rPos;

			//give bullet players velocity
			//if (newBullet.GetComponent<BulletController>().n) {
				newBullet.GetComponent<BulletController>().velocity += owner.GetComponent<PlayerController>().m_currentVelocity;
			//}

			//set who it belongs to
			newBullet.GetComponent<BulletController>().owner = owner;

			//take away ammo if not on screen
			ammo--;
			//effects
			StartCoroutine("ShootFx");
		} else {
			//player cant shoot
			//click sound effect
		}
	}

	public virtual IEnumerator ShootFx () {
		// RECOIL
		if (owner.GetComponent<PlayerController>().m_onGround && shootVector.y > 0) { //shooting on the ground
			owner.GetComponent<PlayerController>().AddForce (new Vector3(-shootVector.x * recoilForce + Random.Range(-recoilForce, recoilForce), -shootVector.y * recoilForce, 0));
		} else {
			owner.GetComponent<PlayerController>().AddForce (-shootVector * recoilForce);
		}

		//screenshake
		cameraController.StartScreenShake(0.1f,2);
		//make sound
		owner.GetComponent<PlayerController>().PlaySound(1); //TODO move to bullet or gun maybe
		//muzzle flash on
		muzzleFlash.SetActive(true);
		//recoil out
		Vector3 recoilDistance = -shootVector * recoilForce * 0.5f;
		transform.position += recoilDistance;
		yield return null;
		yield return null;
		//muzzle flash off
		muzzleFlash.SetActive(false);
		yield return null;
		yield return null;
		//recoil back TODO test
		transform.position -= recoilDistance;
	}

	public void Reload () {
		ammo = maxAmmo;//TODO
	}

	void SetShootDir() {

		string xAxis = owner.GetComponent<PlayerController>().m_horizontalAxis;
		string yAxis = owner.GetComponent<PlayerController>().m_verticalAxis;

		upButton = Input.GetAxisRaw(yAxis) > 0;
		leftButton = Input.GetAxisRaw(xAxis) < 0;
		downButton = Input.GetAxisRaw(yAxis) < 0;
		rightButton = Input.GetAxisRaw(xAxis) > 0;

		if (upButton)
			shootDir = EDir.N;
		if (downButton)
			shootDir = EDir.S;
		if (leftButton)
			shootDir = EDir.W;
		if (rightButton)
			shootDir = EDir.E;

		if (upButton && leftButton)
			shootDir = EDir.NW;
		if (upButton && rightButton)
			shootDir = EDir.NE;
		if (downButton && leftButton)
			shootDir = EDir.SW;
		if (downButton && rightButton)
			shootDir = EDir.SE;

		switch (shootDir) { // TODO maybe i can make an enum that links these two?
			case EDir.N:
				shootVector = Vector3.up;
				break;
			case EDir.NE:
				shootVector = new Vector3(1,1,0);
				break;
			case EDir.E:
				shootVector = Vector3.right;
				break;
			case EDir.SE:
				shootVector = new Vector3(1,-1,0);
				break;
			case EDir.S:
				shootVector = Vector3.down;
				break;
			case EDir.SW:
				shootVector = new Vector3(-1,-1,0);
				break;
			case EDir.W:
				shootVector = Vector3.left;
				break;
			case EDir.NW:
				shootVector = new Vector3(-1,1,0);
				break;
			default:
				shootDir = EDir.N;
				SetRotation();
				break;
		}
	}

	void SetRotation() {
		Vector3 angle = Vector3.zero;

		switch (shootDir) {
			case EDir.N:
				angle.z = 90;
				break;
			case EDir.NE:
				angle.z = 45;
				break;
			case EDir.E:
				angle.z = 0;
				break;
			case EDir.SE:
				angle.z = 315;
				break;
			case EDir.S:
				angle.z = 270;
				break;
			case EDir.SW:
				angle.z = 225;
				break;
			case EDir.W:
				angle.z = 180;
				break;
			case EDir.NW:
				angle.z = 135;	
				break;
			default:
				shootDir = EDir.N;
				SetRotation();
				break;
		}

		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(angle), 0.5f);

		GetComponent<SpriteRenderer>().flipY = (angle.z < 270 && angle.z > 90);

		if (isHeld) {
			transform.position = Vector3.Lerp(transform.position,new Vector3(owner.transform.position.x + owner.GetComponent<PlayerController>().m_lastDirection * 0.25f,owner.transform.position.y, 0),0.75f);
		}

	}

}
