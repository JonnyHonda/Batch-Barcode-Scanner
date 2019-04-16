
# Batch Barcode Scanner App
This application has been designed to work specifically on the **M Series Rugged Android Barcode Scanner** see - https://www.scansku.com/uk/features/. You will need the 2D capable model though. 

### Application features
- Configurable data submission endpoint
- Configurable Regular expression filters
- Export stored data to CSV
- Configurable data retention period
- GPS location of scans
- Security Token support
- Batched uploads
- JSON data submission
- Base64 Encoded Signature images


**Configurable data submission endpoint**
The application is configured by a a QR code, doing allows the application to be configured in a single scan. This code is a simple JSON object that contains the name value pairs of configuration items.

    {
    "UpdateConfiguration":[
	    {
	    "UploadEndPoint":"https://webhook.site/b65ee949-337b-4d55-aaae-dcb2100d3b20",
	    "RegexEndPoint":"http://Test.com/ParcelRegex.json",
	    "ApplicationKey":"60b1a35f-3ced-47df-b33c-10c87d9b64df",
	    "RetentionPeriod":7
	    }
	  ]
	}
	
This JSON object can be converted to a QR code here https://www.barcodesinc.com/generator/qr/

**Configuration features**
*UploadEndPoint*:  This is the location that the application will send the scanned data to, the end point must be capable to receive a payload of application/json and must return a HTTP status code of 200 OK in order for the application to consider the data successfully sent.
  
*RegexEndPoint*:  The application can use regular expressions to filter out unwanted scans. if you don't wish to use this feature set the file on your RegexEndPoint to contain.

    [{
    "all": "/^*/gi"
    }]


*ApplicationKey*: This is a security token that the device can send in order for your receiving application to identify that the data being sent is from a legitimate source, this can be any string.

*RetentionPeriod*: This is simply the number of days that the application will retain the data, every time the application is requested to upload the data it will check and delete any data older than the retention period in days.

**Configurable Regular expression filters** 
Complex regular expressions can be setup to filter out unwanted scans, foe example the following example filters for only these specific courier barcodes.

     [{ 
         "royal-mail": "/^([A-Z]{2}[0-9]{9}GB)/gi",
          "parcelforce-international": "/^((EK|CK){2}[0-9]{9}GB)/gi",
          "parcelforce-domestic": "/(PB[A-Z]{2}[0-9]{10})/gi",
          "yodel": "/^(JJD[0-9]{16})/gi",
          "dhl": "/^(JD[0-9]{18})/gi",
          "whistl": "/^(WSLL10064[0-9]{8})/gi"
        }]

alternatively this regular expression will allow anything to be scanned 

    [{
    "all": "/^*/gi"
    }]

The application will not scan anything without some form of regular expression set, so if you don't have a need to filter out scans or don't know what regular expressions are, use the second example.

**Export stored data to CSV**
Any data that application has will can be exported as CSV file, this file will be downed and stored in the devices default **Downloads** location, a notification will be raised when the download is complete.

**GPS location of scans**
If your device has GPS, this can be enabled and used as part of the uploaded data

**Security Token support**
It is possible to set a configurable value *ApplicationKey* as a security token, the application will send this in the HTTP headers of the data as *x-scansku-api-key*  your receiving application can use this key/value pair to identify that the data is coming from a valid device.

**Batched uploads**
A group of scans taken in one session and then uploaded is called a batch, then a new batch is created ready for the next upload. If an upload was to fail the items are still considered as batched but just not sent and a new batch will still be created. When the application is able to send, all previous batches are sent, each batch will have a unique batch number sent both in the header as *x-scansku-batch* and in the root of the uploaded JSON data as *batch*.

**JSON data submission**
The data is as a JSON  object, the HTTP headers will contain the following additional information
- content-type  application/json
- the device serial number as x-scansku-serial-number
- the batch number as x-scansku-batch
- the user agent as "ScanSKU Handheld Device"
- the application token as x-scansku-api-key

The JSON object will follow the following example

    {
      "timestamp": "2019-03-04T14:07:40",
      "gps": {
        "latitude": 53.2031666666667,
        "longitude": -0.601631666666667
      },
      "batch": "7ddd4005-c25a-4e85-9bef-efe9c7640227",
      "signatureimage": "",
      "signaturetext": "JonnyHonda",
      "scans": [
        {
          "barcode": "PBJY0562315001",
          "timestamp": "2019-03-04T14:06:11",
          "gps": {
            "latitude": 53.20346500000001,
            "longitude": -0.60134
          }
        },
        {
          "barcode": "PBJY0560067001",
          "timestamp": "2019-03-04T14:06:12",
          "gps": {
            "latitude": 53.20347666666667,
            "longitude": -0.6013633333333334
          }
        }
      ]
    }

The *root* object contains the date/time, batch number and gps location of the attempted upload, the *scans* array will contain scanned barcode in the batch, the time it was scanned and the co-ordinates, in addition if a signature was supplied at the time of upload tgis will be included as Base64 encoded text in the SignatureImage field.

