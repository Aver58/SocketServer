// Generated by sprotodump. DO NOT EDIT!
// source: D:\GitRepositories\SocketServer\Tools\sproto2cs\..\\..\\Proto\\ProtoFile\\Common.sproto

using System;
using Sproto;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NetSprotoType { 
	public class Error {
	
		public class response : SprotoTypeBase {
			private static int max_field_count = 2;
			
			
			private Int64 _errorCode; // tag 0
			public Int64 errorCode {
				get { return _errorCode; }
				set { base.has_field.set_field (0, true); _errorCode = value; }
			}
			public bool HasErrorCode {
				get { return base.has_field.has_field (0); }
			}

			private string _errorText; // tag 1
			public string errorText {
				get { return _errorText; }
				set { base.has_field.set_field (1, true); _errorText = value; }
			}
			public bool HasErrorText {
				get { return base.has_field.has_field (1); }
			}

			public response () : base(max_field_count) {}

			public response (byte[] buffer) : base(max_field_count, buffer) {
				this.decode ();
			}

			protected override void decode () {
				int tag = -1;
				while (-1 != (tag = base.deserialize.read_tag ())) {
					switch (tag) {
					case 0:
						this.errorCode = base.deserialize.read_integer ();
						break;
					case 1:
						this.errorText = base.deserialize.read_string ();
						break;
					default:
						base.deserialize.read_unknow_data ();
						break;
					}
				}
			}

			public override int encode (SprotoStream stream) {
				base.serialize.open (stream);

				if (base.has_field.has_field (0)) {
					base.serialize.write_integer (this.errorCode, 0);
				}

				if (base.has_field.has_field (1)) {
					base.serialize.write_string (this.errorText, 1);
				}

				return base.serialize.close ();
			}
		}


	}


	public class RPCParam : SprotoTypeBase {
		private static int max_field_count = 2;
		
		
		private string _method; // tag 0
		public string method {
			get { return _method; }
			set { base.has_field.set_field (0, true); _method = value; }
		}
		public bool HasMethod {
			get { return base.has_field.has_field (0); }
		}

		private string _param; // tag 1
		public string param {
			get { return _param; }
			set { base.has_field.set_field (1, true); _param = value; }
		}
		public bool HasParam {
			get { return base.has_field.has_field (1); }
		}

		public RPCParam () : base(max_field_count) {}

		public RPCParam (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.method = base.deserialize.read_string ();
					break;
				case 1:
					this.param = base.deserialize.read_string ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_string (this.method, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.param, 1);
			}

			return base.serialize.close ();
		}
	}


	public class SocketAccept : SprotoTypeBase {
		private static int max_field_count = 3;
		
		
		private Int64 _connection; // tag 0
		public Int64 connection {
			get { return _connection; }
			set { base.has_field.set_field (0, true); _connection = value; }
		}
		public bool HasConnection {
			get { return base.has_field.has_field (0); }
		}

		private string _ip; // tag 1
		public string ip {
			get { return _ip; }
			set { base.has_field.set_field (1, true); _ip = value; }
		}
		public bool HasIp {
			get { return base.has_field.has_field (1); }
		}

		private Int64 _port; // tag 2
		public Int64 port {
			get { return _port; }
			set { base.has_field.set_field (2, true); _port = value; }
		}
		public bool HasPort {
			get { return base.has_field.has_field (2); }
		}

		public SocketAccept () : base(max_field_count) {}

		public SocketAccept (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.connection = base.deserialize.read_integer ();
					break;
				case 1:
					this.ip = base.deserialize.read_string ();
					break;
				case 2:
					this.port = base.deserialize.read_integer ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_integer (this.connection, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.ip, 1);
			}

			if (base.has_field.has_field (2)) {
				base.serialize.write_integer (this.port, 2);
			}

			return base.serialize.close ();
		}
	}


	public class SocketData : SprotoTypeBase {
		private static int max_field_count = 2;
		
		
		private Int64 _connection; // tag 0
		public Int64 connection {
			get { return _connection; }
			set { base.has_field.set_field (0, true); _connection = value; }
		}
		public bool HasConnection {
			get { return base.has_field.has_field (0); }
		}

		private string _buffer; // tag 1
		public string buffer {
			get { return _buffer; }
			set { base.has_field.set_field (1, true); _buffer = value; }
		}
		public bool HasBuffer {
			get { return base.has_field.has_field (1); }
		}

		public SocketData () : base(max_field_count) {}

		public SocketData (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.connection = base.deserialize.read_integer ();
					break;
				case 1:
					this.buffer = base.deserialize.read_string ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_integer (this.connection, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_string (this.buffer, 1);
			}

			return base.serialize.close ();
		}
	}


	public class SocketError : SprotoTypeBase {
		private static int max_field_count = 4;
		
		
		private Int64 _connection; // tag 0
		public Int64 connection {
			get { return _connection; }
			set { base.has_field.set_field (0, true); _connection = value; }
		}
		public bool HasConnection {
			get { return base.has_field.has_field (0); }
		}

		private Int64 _errorCode; // tag 1
		public Int64 errorCode {
			get { return _errorCode; }
			set { base.has_field.set_field (1, true); _errorCode = value; }
		}
		public bool HasErrorCode {
			get { return base.has_field.has_field (1); }
		}

		private string _errorText; // tag 2
		public string errorText {
			get { return _errorText; }
			set { base.has_field.set_field (2, true); _errorText = value; }
		}
		public bool HasErrorText {
			get { return base.has_field.has_field (2); }
		}

		private string _remoteEndPoint; // tag 3
		public string remoteEndPoint {
			get { return _remoteEndPoint; }
			set { base.has_field.set_field (3, true); _remoteEndPoint = value; }
		}
		public bool HasRemoteEndPoint {
			get { return base.has_field.has_field (3); }
		}

		public SocketError () : base(max_field_count) {}

		public SocketError (byte[] buffer) : base(max_field_count, buffer) {
			this.decode ();
		}

		protected override void decode () {
			int tag = -1;
			while (-1 != (tag = base.deserialize.read_tag ())) {
				switch (tag) {
				case 0:
					this.connection = base.deserialize.read_integer ();
					break;
				case 1:
					this.errorCode = base.deserialize.read_integer ();
					break;
				case 2:
					this.errorText = base.deserialize.read_string ();
					break;
				case 3:
					this.remoteEndPoint = base.deserialize.read_string ();
					break;
				default:
					base.deserialize.read_unknow_data ();
					break;
				}
			}
		}

		public override int encode (SprotoStream stream) {
			base.serialize.open (stream);

			if (base.has_field.has_field (0)) {
				base.serialize.write_integer (this.connection, 0);
			}

			if (base.has_field.has_field (1)) {
				base.serialize.write_integer (this.errorCode, 1);
			}

			if (base.has_field.has_field (2)) {
				base.serialize.write_string (this.errorText, 2);
			}

			if (base.has_field.has_field (3)) {
				base.serialize.write_string (this.remoteEndPoint, 3);
			}

			return base.serialize.close ();
		}
	}


}

