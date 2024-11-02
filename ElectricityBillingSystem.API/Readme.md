---

# Electricity Bill Payment System

This project is a backend service for managing electricity bill payments using an event-driven architecture. It includes features for creating and paying electricity bills, managing user wallets, and sending notifications upon certain events.

## Project Structure

- **Event Bus**: `EventService` - Publishes events to AWS SNS for event-driven architecture.
- **Event Producers**:
  - `BillService` - Handles bill creation and payment, publishing `bill_created` and `payment_completed` events.
  - `WalletService` - Manages wallet balance and could publish wallet-related events if extended.
- **Event Consumer**: `NotificationService` - A background service that listens to AWS SQS for events and sends SMS notifications based on the event type.

## Features
- **User Management**: Login and logout user for authentication.
- **Bill Management**: Create and pay bills.
- **Wallet Management**: Add funds and manage balances.
- **Event-Driven Notifications**: Events trigger notifications via SMS.
- **AWS Integration**: Uses SNS(Mock Implementation) for event publishing and SQS for message consumption.
- **SMS Notifications**: Uses Twilio(Mock Implementation) for sending SMS messages.

## Technologies

- **Framework**: ASP.NET Core 6.0
- **Cloud Services**: AWS SNS, SQS (using LocalStack for local testing)
- **SMS API**: Twilio
- **Messaging**: Event-driven design with AWS SNS and SQS

## Getting Started

### Prerequisites

- **.NET 6 SDK**
- **AWS Account** with SNS and SQS services (or LocalStack for local testing)
- **Twilio Account** for SMS notifications

### Setup

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/your-username/electricity-bill-payment-system.git
   cd electricity-bill-payment-system
   ```

2. **Install Dependencies**:
   Ensure you have the required NuGet packages by running:
   ```bash
   dotnet restore
   ```

3. **AWS Configuration**:
   Configure your AWS credentials either via the AWS CLI or by setting environment variables. Alternatively, use LocalStack for local SNS and SQS testing.

4. **Set Up Twilio Credentials**:
   Add your Twilio `AccountSid` and `AuthToken` to `appsettings.json`:
   ```json
   {
     "Twilio": {
       "AccountSid": "your_account_sid",
       "AuthToken": "your_auth_token"
     }
   }
   ```

5. **Update AWS Configurations**:
   Add your SQS Queue URL and region to `appsettings.json`:
   ```json
   {
     "AWS": {
       "Region": "us-east-1",
       "SQSQueueUrl": "https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue" // Replace with your SQS queue URL
     }
   }
   ```

6. **Run the Application**:
   Start the ASP.NET Core application:
   ```bash
   dotnet run
   ```

### Usage

#### API Endpoints

- **POST /api/electricity/verify**: Creates a new electricity bill.
  - **Body**: `{"meterNumber": "A123456", "amount": 100.00}`

- **POST /api/Vend/{billId}/pay**: Processes a payment for a specific bill.
  - **Parameters**: `billId` (GUID)

- **POST /api/wallets/{walletId}/add-funds**: Adds funds to a user’s wallet.
  - **Parameters**: `walletId` (GUID)
  - **Body**: `{ "amount": 100.0 }`

#### Event-Driven Notifications

- When a bill is created, an event (`bill_created`) is published to SNS, which is then consumed by the `NotificationService` via SQS.
- When a bill is paid, an event (`payment_completed`) is published, triggering the consumer to send a payment confirmation SMS.

### Architecture

- **Event Bus**: Manages event publishing using SNS. All events are sent to `EventService`, which is responsible for routing them.
- **Event Producer(s)**:
  - `BillService` produces events for bill creation and successful payment.
  - `WalletService` manages funds and could trigger wallet-related events if extended.
- **Event Consumer(s)**: 
  - `NotificationService`: A background service that consumes messages from SQS. It acts on different event types (e.g., `bill_created`, `payment_completed`) to send SMS notifications using Twilio.

### Testing the API

You can use Postman or `curl` to test the API endpoints:

- **Create a New Bill**:
  ```bash
  curl -X POST "http://localhost:5000/api/electricity/verify" -H "Content-Type: application/json" -d "{"meterNumber": "A123456", "amount": 100.00}"
  ```

- **Pay a Bill**:
  ```bash
  curl -X POST "http://localhost:5000/api/Vend/{billId}/pay" -H "Content-Type: application/json" -d "{\"amount\": 150.0}"
  ```

- **Add Funds to Wallet**:
  ```bash
  curl -X POST "http://localhost:5000/api/wallets/{walletId}/add-funds" -H "Content-Type: application/json" -d "{\"amount\": 50.0}"
  ```

### Configuration and Usage

- **AWS SNS and SQS**: Used to publish and consume events. LocalStack can be used for local testing if AWS credentials are unavailable.
- **Twilio SMS Notifications**: Sends SMS on specific events, including bill creation and successful payment. Configure Twilio by setting `AccountSid` and `AuthToken` in `appsettings.json`.

### Observability and Logging

To monitor event flows and debug any issues:
1. **SNS and SQS Logs**: Use AWS CloudWatch to view logs for SNS and SQS if connected to AWS.
2. **ASP.NET Core Logs**: Use `ILogger` in each service for logging messages, errors, and information regarding event processing.
3. **Twilio Logs**: Use Twilio's dashboard to monitor SMS notifications sent from the application.

### Design Decisions

- **Event-Driven Architecture**: Allows asynchronous processing, decoupling services for scalability and easier maintainability.
- **AWS SNS and SQS**: Selected for robust event messaging and pub-sub pattern. Allows for extending consumers in the future for additional functionality.
- **Twilio SMS Notifications**: Used for reliable messaging with configurable tokens.

### Challenges and Solutions

- **Message Duplication**: Messages in SQS may appear more than once. This was handled by checking the event type and ID before re-processing.
- **Testing Event Flow Locally**: LocalStack was configured to simulate SNS and SQS, enabling full local testing without connecting to AWS.

### Future Enhancements

- **Additional Event Consumers**: Implement more consumers, like an `AuditService` for logging and a `PaymentNotificationService` for external notification integrations.
- **Third-Party API Integration**: Connect to real electricity providers for bill verification and processing.

### Contributing

Contributions are welcome. Please open an issue or submit a pull request with details of the proposed changes.

### License

This project is licensed under the MIT License.

---
