package autox.tests;

import autox.config.Configuration;
import autox.log.Log;
import autox.utils.Cipher;
import org.apache.axis.encoding.Base64;



public class MainTest {

    public static void main(String[] unused) throws Exception {

        // Generate a key-pair
        Log.debug("Before generate key pair");

        Cipher.Keys keys = new Cipher.Keys().generateKeyPair();

        String publicKeyString = Configuration.getInstance().get("key.public","");
        String privateKeyString = Configuration.getInstance().get("key.private", "");


        String target =   String.valueOf("Jien Huang is a good QA. He is doing 中文测试！".hashCode());

        Log.debug("our target is:"+target);

        String encBytes = Cipher.encrypt( target);
        Log.debug("after encrypt:"+encBytes);
        byte[] result =Cipher.decrypt(Base64.decode(encBytes), Cipher.getPrivateKeyFromString(privateKeyString),Cipher.ALGORITHM);
        Log.debug("middle result:"+String.valueOf(result));
        String decBytes = Cipher.getFromBASE64(Base64.encode(result));
        Log.debug("after decrypt:"+ decBytes );

        boolean expected = decBytes.equals(target)   ;

        Log.debug("Test1 " + (expected ? "SUCCEEDED!" : "FAILED!"));

        encBytes = Base64.encode(Cipher.encrypt(target.getBytes(),Cipher.getPrivateKeyFromString(privateKeyString),Cipher.ALGORITHM));
        Log.debug("after encrypt:"+encBytes);
        decBytes = Cipher.decrypt(encBytes);

        Log.debug("after decrypt:"+ decBytes );
        expected = decBytes.equals(target)   ;

        Log.debug("Test1 " + (expected ? "SUCCEEDED!" : "FAILED!"));

    }


}
