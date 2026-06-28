import { useCallback, useEffect, useState } from 'react';
import { api, CheckoutStatusLabel, type CheckoutState, type Product, type PurchaseIntentState } from './api';
import { Button } from './components/ui/button';
import { Card, CardContent, CardHeader } from './components/ui/card';
import { Input } from './components/ui/input';

// CheckoutStatus enum values mirrored from the server contract
const CHECKOUT_STATUS_PAYMENT_DECLINED = 7;
const CHECKOUT_STATUS_COMPLETED = 4;
const CHECKOUT_STATUS_FAILED = 5;

function canCancelCheckout(status: number) {
  return status < CHECKOUT_STATUS_COMPLETED || status === CHECKOUT_STATUS_PAYMENT_DECLINED;
}

export default function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [pi, setPi] = useState<PurchaseIntentState | null>(null);
  const [checkout, setCheckout] = useState<CheckoutState | null>(null);
  const [email, setEmail] = useState('');
  const [emailSent, setEmailSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [shippingForm, setShippingForm] = useState({ fullName: '', line1: '', city: '', postalCode: '', country: 'GB' });
  const [paymentForm, setPaymentForm] = useState({ cardHolder: '', maskedNumber: '4242 4242 4242 4242', expiryMonth: '12', expiryYear: '2027' });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [prods, intent] = await Promise.all([api.getProducts(), api.getPurchaseIntent()]);
      setProducts(prods);
      setPi(intent);
      if (intent.checkoutId) {
        const currentCheckout = await api.getCheckout(intent.checkoutId);
        setCheckout(currentCheckout);
      } else {
        setCheckout(null);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const handle = (fn: () => Promise<unknown>) => async () => {
    setError(null);
    try {
      await fn();
      await load();
    } catch (e) {
      setError(String(e));
    }
  };

  const total = pi?.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0) ?? 0;

  return (
    <div className="max-w-screen-xl mx-auto p-6 font-sans text-gray-900 bg-gray-50 min-h-screen">
      <header className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold m-0">⚡ Temporal Nexus Demo Shop</h1>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={handle(() => api.newCleanCustomer())}>
            New Customer
          </Button>
          <a href="http://localhost:8025" target="_blank" rel="noreferrer">
            <Button variant="outline" size="sm" type="button">📧 MailPit</Button>
          </a>
        </div>
      </header>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-md mb-4">
          {error}
        </div>
      )}
      {loading && <p className="text-gray-500 text-sm">Loading…</p>}

      <div className="grid grid-cols-[2fr_1fr] gap-6">
        {/* Product catalogue */}
        <div>
          <h2 className="text-xl font-semibold mb-3">Products</h2>
          <div className="grid grid-cols-2 gap-4">
            {products.map((product) => (
              <Card key={product.sku}>
                <CardContent className="pt-4">
                  <strong className="block text-sm font-semibold">{product.name}</strong>
                  <p className="text-gray-500 text-xs mt-1">{product.description}</p>
                  <p className="font-bold mt-2">${product.price.toFixed(2)}</p>
                  <Button
                    className="mt-3 w-full"
                    onClick={handle(() => api.addItem(product.sku, 1))}
                  >
                    Add to Basket
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        {/* Basket + checkout sidebar */}
        <div>
          <h2 className="text-xl font-semibold mb-3">
            Basket {pi?.items.length ? `(${pi.items.length} items)` : ''}
          </h2>

          {!pi?.items.length ? (
            <p className="text-gray-400 text-sm">Empty</p>
          ) : (
            <>
              {pi.items.map((item) => (
                <div key={item.sku} className="flex justify-between items-center mb-2 gap-2 text-sm">
                  <span>{item.name} ×{item.quantity}</span>
                  <span className="text-gray-600">${(item.unitPrice * item.quantity).toFixed(2)}</span>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={handle(() => api.removeItem(item.sku))}
                    className="text-red-500 hover:text-red-700 hover:bg-red-50"
                  >
                    ✕
                  </Button>
                </div>
              ))}
              <p className="font-bold border-t border-gray-200 pt-2 mt-2">Total: ${total.toFixed(2)}</p>
            </>
          )}

          <h3 className="text-base font-semibold mt-4 mb-2">Email</h3>
          {pi?.email ? (
            <p className="text-sm">
              {pi.email}{' '}
              {pi.emailVerified
                ? <span className="text-green-600">✅ verified</span>
                : <span className="text-amber-600">⏳ unverified — check MailPit</span>}
            </p>
          ) : (
            <div className="flex gap-2">
              <Input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
              />
              <Button
                size="sm"
                onClick={handle(async () => {
                  await api.provideEmail(email);
                  setEmailSent(true);
                })}
              >
                Verify
              </Button>
            </div>
          )}
          {emailSent && !pi?.emailVerified && (
            <p className="text-xs text-gray-500 mt-1">📬 Check MailPit for the verification link.</p>
          )}

          {!pi?.checkoutId && (
            <Button
              variant="success"
              size="lg"
              className="w-full mt-4"
              onClick={handle(() => api.startCheckout())}
            >
              Start Checkout
            </Button>
          )}

          {checkout && (
            <Card className="mt-4">
              <CardHeader className="pb-2">
                <h3 className="text-base font-semibold">
                  Checkout:{' '}
                  <span className={checkout.status === CHECKOUT_STATUS_COMPLETED ? 'text-green-600' : checkout.status >= CHECKOUT_STATUS_FAILED ? 'text-red-600' : 'text-blue-600'}>
                    {CheckoutStatusLabel[checkout.status] ?? `Status ${checkout.status}`}
                  </span>
                </h3>
                {checkout.failureReason && (
                  <p className="text-red-600 text-sm">{checkout.failureReason}</p>
                )}
              </CardHeader>
              <CardContent>
                {!checkout.shippingAddress && checkout.status === 0 && (
                  <div>
                    <h4 className="text-sm font-semibold mb-2">Shipping Address</h4>
                    {(['fullName', 'line1', 'city', 'postalCode'] as const).map((field) => (
                      <Input
                        key={field}
                        className="mb-1.5"
                        value={shippingForm[field]}
                        onChange={(e) => setShippingForm((s) => ({ ...s, [field]: e.target.value }))}
                        placeholder={{ fullName: 'Full Name', line1: 'Address Line 1', city: 'City', postalCode: 'Postal Code' }[field]}
                      />
                    ))}
                    <Button
                      size="sm"
                      className="mt-1"
                      onClick={handle(() => api.provideShipping(checkout.checkoutId, { ...shippingForm }))}
                    >
                      Save Address
                    </Button>
                  </div>
                )}

                {!checkout.payment && checkout.status === 0 && (
                  <div className="mt-3">
                    <h4 className="text-sm font-semibold mb-2">Payment</h4>
                    <Input
                      className="mb-1.5"
                      value={paymentForm.cardHolder}
                      onChange={(e) => setPaymentForm((p) => ({ ...p, cardHolder: e.target.value }))}
                      placeholder="Cardholder name"
                    />
                    <Button
                      size="sm"
                      className="mt-1"
                      onClick={handle(() => api.providePayment(checkout.checkoutId, { ...paymentForm }))}
                    >
                      Save Payment
                    </Button>
                  </div>
                )}

                <div className="flex gap-2 mt-3 flex-wrap">
                  {checkout.status === CHECKOUT_STATUS_PAYMENT_DECLINED && (
                    <Button
                      variant="warning"
                      size="sm"
                      onClick={handle(() => api.retryPayment(checkout.checkoutId))}
                    >
                      Retry Payment
                    </Button>
                  )}
                  {canCancelCheckout(checkout.status) && (
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={handle(() => api.cancelCheckout(checkout.checkoutId))}
                    >
                      Cancel
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {pi && (
        <div className="mt-6 bg-gray-100 rounded-lg px-4 py-3 text-xs text-gray-500">
          <strong>Debug:</strong> Purchase Intent: {pi.purchaseIntentId} · Status: {pi.status}
        </div>
      )}
    </div>
  );
}
